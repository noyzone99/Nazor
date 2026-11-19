import argparse
import io
import json
import re
import xml.etree.ElementTree as ET
from importlib.metadata import version
from pathlib import Path

import skia
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.basePen import BasePen
from fontTools.pens.areaPen import AreaPen
from fontTools.pens.reverseContourPen import ReverseContourPen
from fontTools.pens.t2CharStringPen import T2CharStringPen
from fontTools.pens.transformPen import TransformPen
from fontTools.svgLib.path import parse_path


class PathPen(BasePen):
    def __init__(self):
        super().__init__(None)
        self.path = skia.Path()

    def _moveTo(self, point):
        self.path.moveTo(*point)

    def _lineTo(self, point):
        self.path.lineTo(*point)

    def _curveToOne(self, first, second, end):
        self.path.cubicTo(*first, *second, *end)

    def _qCurveToOne(self, control, end):
        self.path.quadTo(*control, *end)

    def _closePath(self):
        self.path.close()

    def _endPath(self):
        pass


def outline(file):
    root = ET.parse(file).getroot()
    if root.tag != "{http://www.w3.org/2000/svg}svg" or root.attrib.get("viewBox") != "0 0 24 24":
        raise ValueError(f"Expected a 24 by 24 SVG: {file}")
    supported = {"viewBox", "fill", "stroke", "stroke-width", "stroke-linecap", "stroke-linejoin"}
    if set(root.attrib) - supported:
        raise ValueError(f"Unsupported SVG root attributes: {file}")
    result = skia.Path()
    for element in root:
        tag = element.tag.rsplit("}", 1)[-1]
        attributes = root.attrib | element.attrib
        if list(element) or tag not in ("path", "circle", "rect"):
            raise ValueError(f"Unsupported SVG shape: {file}: {tag}")
        allowed = {"d"} if tag == "path" else {"cx", "cy", "r"} if tag == "circle" else {"x", "y", "width", "height", "rx", "ry"}
        if set(element.attrib) - allowed - {"fill", "stroke", "stroke-width"}:
            raise ValueError(f"Unsupported SVG shape attributes: {file}: {tag}")
        path = skia.Path()
        if tag == "path":
            pen = PathPen()
            parse_path(attributes["d"], pen)
            path = pen.path
        elif tag == "circle":
            path.addCircle(float(attributes["cx"]), float(attributes["cy"]), float(attributes["r"]))
        else:
            number = lambda key, default=0: float(attributes.get(key, default))
            path.addRoundRect(skia.Rect.MakeXYWH(number("x"), number("y"), number("width"), number("height")),
                              number("rx"), number("ry", number("rx")))
        if attributes.get("fill", "black") not in ("none", "black") or attributes.get("stroke", "none") not in ("none", "black"):
            raise ValueError(f"Expected monochrome SVG artwork: {file}")
        if attributes.get("fill", "black") == "black":
            result = skia.Op(result, path, skia.PathOp.kUnion_PathOp)
        if attributes.get("stroke", "none") == "black":
            if attributes.get("stroke-linecap") != "round" or attributes.get("stroke-linejoin") != "round":
                raise ValueError(f"Expected round SVG strokes: {file}")
            paint = skia.Paint(Style=skia.Paint.kStroke_Style, StrokeWidth=float(attributes["stroke-width"]),
                               StrokeCap=skia.Paint.kRound_Cap, StrokeJoin=skia.Paint.kRound_Join)
            filled = skia.Path()
            if not paint.getFillPath(path, filled, resScale=4):
                raise ValueError(f"Cannot expand SVG stroke: {file}")
            result = skia.Op(result, filled, skia.PathOp.kUnion_PathOp)
    if result.isEmpty():
        raise ValueError(f"Empty SVG icon: {file}")
    return result


def draw_path(path, pen):
    iterator = skia.Path.RawIter(path)
    point = lambda p: (p.x(), p.y())
    for verb, points in iterator:
        if verb == skia.Path.kMove_Verb:
            pen.moveTo(point(points[0]))
        elif verb == skia.Path.kLine_Verb:
            pen.lineTo(point(points[1]))
        elif verb == skia.Path.kQuad_Verb:
            pen.qCurveTo(*map(point, points[1:]))
        elif verb == skia.Path.kCubic_Verb:
            pen.curveTo(*map(point, points[1:]))
        elif verb == skia.Path.kConic_Verb:
            quads = skia.Path.ConvertConicToQuads(*points, iterator.conicWeight(), 3)
            for index in range(1, len(quads), 2):
                pen.qCurveTo(point(quads[index]), point(quads[index + 1]))
        elif verb == skia.Path.kClose_Verb:
            pen.closePath()
        else:
            raise ValueError(f"Unexpected Skia path verb: {verb}")


def font_glyph(path):
    simplified = skia.Simplify(path)
    measure = skia.PathMeasure(simplified, True)
    contours = []
    while True:
        if measure.getLength() > 0:
            contour = skia.Path()
            measure.getSegment(0, measure.getLength(), contour, True)
            contour.close()
            contours.append(contour)
        if not measure.nextContour():
            break
    pen = T2CharStringPen(1000, None, roundTolerance=0.01)
    transformed = TransformPen(pen, (1000 / 24, 0, 0, -1000 / 24, 0, 1000))
    for contour in contours:
        point = contour.getPoint(0)
        depth = sum(other.contains(point.x(), point.y()) for other in contours if other is not contour)
        area = AreaPen(None)
        draw_path(contour, area)
        output = transformed if (area.value > 0) == (depth % 2 == 0) else ReverseContourPen(transformed)
        draw_path(contour, output)
    return pen.getCharString()


def generate(root):
    theme = root / "Sources/Themes/Nazor.Themes.NazorLeaf"
    symbols = sorted(json.loads((theme / "Catalog/Symbols.json").read_text(encoding="utf-8")), key=lambda symbol: symbol["name"])
    names, scalars, files = set(), set(), set()
    paths = {}
    for symbol in symbols:
        name, scalar, file = symbol["name"], symbol["codepoint"], symbol["file"]
        if not re.fullmatch(r"[A-Z][A-Za-z0-9]*", name) or name in ("FontFamily", "Glyph", "LeafSymbol") or name in names:
            raise ValueError(f"Invalid or duplicate symbol name: {name}")
        if not isinstance(scalar, int) or not 0xE000 <= scalar <= 0xF8FF or scalar in scalars:
            raise ValueError(f"Invalid or duplicate private-use codepoint: {scalar}")
        if not re.fullmatch(r"[a-z]+\.svg", file) or file in files:
            raise ValueError(f"Invalid or duplicate SVG file: {file}")
        names.add(name)
        scalars.add(scalar)
        files.add(file)
        paths[name] = outline(theme / "Icons" / file)
    if files != {file.name for file in (theme / "Icons").glob("*.svg")}:
        raise ValueError("The symbol map must cover every source SVG exactly once.")
    builder = FontBuilder(1000, isTTF=False)
    glyph_order = [".notdef", "space", *paths]
    builder.setupGlyphOrder(glyph_order)
    builder.setupCharacterMap({32: "space"} | {symbol["codepoint"]: symbol["name"] for symbol in symbols})
    outlines = {".notdef": font_glyph(skia.Path()), "space": font_glyph(skia.Path())}
    outlines.update({name: font_glyph(path) for name, path in paths.items()})
    builder.setupCFF("NazorLeafIcons", {"FullName": "NazorLeaf Icons", "FamilyName": "NazorLeaf Icons", "Weight": "Regular", "Notice": "Copyright (c) Nazor contributors. MIT License."}, outlines, {})
    builder.setupHorizontalMetrics({name: (1000, round(paths[name].computeTightBounds().left() * 1000 / 24) if name in paths else 0) for name in glyph_order})
    builder.setupHorizontalHeader(ascent=1000, descent=0)
    builder.setupNameTable({"familyName": "NazorLeaf Icons", "styleName": "Regular", "uniqueFontIdentifier": "NazorLeaf Icons 1.0", "fullName": "NazorLeaf Icons", "psName": "NazorLeafIcons", "version": "Version 1.0", "copyright": "Copyright (c) Nazor contributors", "licenseDescription": "MIT License. See LICENSE-NazorLeaf-Icons.txt distributed with this font."})
    builder.setupOS2(version=4, sTypoAscender=1000, sTypoDescender=0, sTypoLineGap=0, usWinAscent=1000, usWinDescent=0, fsSelection=0xC0)
    builder.setupPost()
    builder.font.recalcTimestamp = False
    builder.font["head"].created = builder.font["head"].modified = 2082844800
    buffer = io.BytesIO()
    builder.font.save(buffer)
    code = "// <auto-generated/>\nnamespace Nazor.Themes.NazorLeaf;\n\npublic readonly partial struct LeafSymbol\n{\n"
    rows = []
    for symbol in symbols:
        name, glyph = symbol["name"], f'\\u{symbol["codepoint"]:04X}'
        code += f'    /// <summary>Gets the {name} icon.</summary>\n    public static LeafSymbol {name} => new("{glyph}");\n\n'
        rows.append(f'| `public static LeafSymbol {name} {{ get; }}` | NazorLeaf icon, glyph `{glyph}`. |')
    reference = root / "Docs/ApiReferences/Nazor.Themes.NazorLeaf.md"
    markdown = reference.read_text(encoding="utf-8")
    pattern = r"(?s)(<!-- leaf-symbol-catalog:start -->\n).*?(<!-- leaf-symbol-catalog:end -->)"
    if len(re.findall(pattern, markdown)) != 1:
        raise ValueError("Expected one Leaf symbol reference marker pair.")
    markdown = re.sub(pattern, lambda match: match[1] + "\n".join(rows) + "\n" + match[2], markdown)
    return {theme / "Fonts/NazorLeafIcons.otf": buffer.getvalue(), theme / "LeafSymbol.g.cs": (code + "}\n").encode(), reference: markdown.encode()}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate the NazorLeaf icon font, typed symbols and API rows from theme SVGs.")
    parser.add_argument("--check", action="store_true", help="Compare generated bytes without writing files.")
    options = parser.parse_args()
    for package, expected in (("fonttools", "4.68.0"), ("skia-python", "144.1")):
        if version(package) != expected:
            raise RuntimeError(f"Use the pinned generation dependencies: {package}=={expected}")
    outputs = generate(Path(__file__).resolve().parents[2])
    for file, data in outputs.items():
        if options.check:
            actual = file.read_bytes() if file.exists() else None
            if actual is not None and file.suffix != ".otf":
                actual = actual.replace(b"\r\n", b"\n")
            if actual != data:
                raise RuntimeError(f"Generated NazorLeaf asset is stale: {file}")
        else:
            file.write_bytes(data)
    print("NazorLeaf: icon font, typed catalog and API rows verified.")
