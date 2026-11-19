import argparse
import hashlib
import json
import math
import platform
import struct
import subprocess
import sys
import xml.etree.ElementTree as ET
import zlib
from datetime import datetime, timezone
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]


def sha(data):
    return hashlib.sha256(data).hexdigest()


def git(*args):
    return subprocess.check_output(['git', '-c', 'core.safecrlf=false', '-C', str(ROOT), *args])


def fingerprint():
    diff = git('diff', 'HEAD', '--binary', '--no-ext-diff')
    extra = []
    for value in sorted(git('ls-files', '--others', '--exclude-standard', '-z').split(b'\0')):
        if value:
            name = value.decode('utf-8')
            file = ROOT / name
            extra.append({'path': name, 'sha256': sha(file.read_bytes())})
    return {'revision': git('rev-parse', 'HEAD').decode().strip(),
            'dirtyFingerprint': sha(diff + json.dumps(extra, sort_keys=True).encode()),
            'trackedChanges': bool(diff), 'untrackedFiles': extra}


def png_read(file):
    data = file.read_bytes()
    if data[:8] != b'\x89PNG\r\n\x1a\n':
        raise ValueError('Lossless PNG input required: ' + str(file))
    cursor, compressed, ended = 8, bytearray(), False
    width = height = channels = None
    while cursor < len(data):
        size = struct.unpack('>I', data[cursor:cursor + 4])[0]
        kind = data[cursor + 4:cursor + 8]
        payload = data[cursor + 8:cursor + 8 + size]
        crc = struct.unpack('>I', data[cursor + 8 + size:cursor + 12 + size])[0]
        if zlib.crc32(kind + payload) & 0xffffffff != crc:
            raise ValueError('Invalid PNG checksum: ' + str(file))
        if kind == b'IHDR':
            width, height, depth, color, compression, filtering, interlace = struct.unpack('>IIBBBBB', payload)
            if depth != 8 or color not in (2, 6) or compression or filtering or interlace:
                raise ValueError('Expected noninterlaced 8-bit RGB/RGBA PNG: ' + str(file))
            channels = 3 if color == 2 else 4
            if width <= 0 or height <= 0 or width * height > 32000000:
                raise ValueError('Capture dimensions must be positive and at most32 million pixels.')
        elif kind == b'IDAT':
            compressed.extend(payload)
        elif kind == b'IEND':
            ended = True
            break
        elif kind == b'tRNS':
            raise ValueError('Use explicit RGBA for transparent captures.')
        cursor += size + 12
    if channels is None or not ended:
        raise ValueError('Incomplete PNG capture.')
    stride = width * channels
    raw = zlib.decompress(compressed)
    if len(raw) != (stride + 1) * height:
        raise ValueError('Invalid PNG scanline length.')
    rgba, previous = bytearray(), bytearray(stride)
    for y in range(height):
        filtering = raw[y * (stride + 1)]
        row = bytearray(raw[y * (stride + 1) + 1:(y + 1) * (stride + 1)])
        if filtering > 4:
            raise ValueError('Invalid PNG filter.')
        for x in range(stride):
            a = row[x - channels] if x >= channels else 0
            b = previous[x]
            c = previous[x - channels] if x >= channels else 0
            if filtering == 1:
                value = a
            elif filtering == 2:
                value = b
            elif filtering == 3:
                value = (a + b) // 2
            elif filtering == 4:
                p = a + b - c
                distances = [abs(p - a), abs(p - b), abs(p - c)]
                value = (a, b, c)[distances.index(min(distances))]
            else:
                value = 0
            row[x] = (row[x] + value) & 255
        if channels == 4:
            rgba.extend(row)
        else:
            for x in range(0, stride, 3):
                rgba.extend(row[x:x + 3] + b'\xff')
        previous = row
    return width, height, rgba


def png_write(file, width, height, rgba):
    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data) & 0xffffffff)
    raw = b''.join(b'\0' + rgba[y * width * 4:(y + 1) * width * 4] for y in range(height))
    file.write_bytes(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(raw)) + chunk(b'IEND', b''))


def compare(reference, actual, output):
    descriptors = [json.loads(file.read_text(encoding='utf-8')) for file in (reference, actual)]
    required = ['scenario', 'platformBuild', 'dpiScale', 'clientSize', 'units', 'fontHashes', 'culture', 'theme', 'colorScheme', 'accent', 'density', 'contrast', 'input', 'motion', 'timeMilliseconds']
    for key in required:
        values = [descriptor.get(key) for descriptor in descriptors]
        if any(value is None for value in values) or values[0] != values[1]:
            raise ValueError('Unknown or mismatched capture metadata: ' + key)
    for descriptor in descriptors:
        for key in ('scenario', 'platformBuild', 'units', 'culture', 'theme', 'colorScheme', 'accent', 'density', 'contrast', 'input', 'motion', 'renderer', 'sourceRevision', 'referenceVersion'):
            if not isinstance(descriptor.get(key), str) or not descriptor[key].strip():
                raise ValueError('Missing capture identity: ' + key)
        capture = descriptor.get('capture', {})
        if capture.get('lifecycleChangedDuringScreenshot') or capture.get('candidateStatus', 'manual-css-candidate') != 'manual-css-candidate':
            raise ValueError('Unstable source lifecycle requires review before pixel comparison.')
        for key in ('renderer', 'sourceRevision', 'referenceVersion', 'clockResolutionMilliseconds'):
            if descriptor.get(key) is None:
                raise ValueError('Missing capture provenance: ' + key)
        for key in ('dpiScale', 'clockResolutionMilliseconds'):
            value = descriptor[key]
            if type(value) not in (int, float) or not math.isfinite(value) or value <= 0:
                raise ValueError('Invalid capture metric: ' + key)
        value = descriptor['timeMilliseconds']
        if type(value) not in (int, float) or not math.isfinite(value) or value < 0:
            raise ValueError('Invalid capture time.')
        fonts = descriptor['fontHashes']
        if not isinstance(fonts, dict) or not fonts or any(not isinstance(value, str) or len(value) != 64 or any(char not in '0123456789abcdefABCDEF' for char in value) for value in fonts.values()):
            raise ValueError('Capture font hashes must identify actual bytes.')
        size = descriptor['clientSize']
        if not isinstance(size, list) or len(size) != 2 or any(type(value) is not int or value <= 0 for value in size):
            raise ValueError('Invalid logical client size.')
    images = [png_read(file.parent / descriptor['image']) for file, descriptor in zip((reference, actual), descriptors)]
    for descriptor, image in zip(descriptors, images):
        units = descriptor['units']
        if units not in ('logical-pixels', 'physical-pixels'):
            raise ValueError('Unsupported capture units: ' + str(units))
        scale = descriptor['dpiScale'] if units == 'logical-pixels' else 1
        expected = tuple(math.ceil(value * scale) for value in descriptor['clientSize'])
        if image[:2] != expected:
            raise ValueError('PNG dimensions do not match the declared client size and DPI.')
    width, height, pixels = images[0]
    if images[1][:2] != (width, height):
        raise ValueError('Different physical capture sizes.')
    other, difference = images[1][2], bytearray(len(pixels))
    count = maximum = 0
    x0, y0, x1, y1 = width, height, -1, -1
    for pixel in range(width * height):
        start = pixel * 4
        delta = max(abs(pixels[start + channel] - other[start + channel]) for channel in range(4))
        if delta:
            count += 1
            maximum = max(maximum, delta)
            x, y = pixel % width, pixel // width
            x0, y0, x1, y1 = min(x0, x), min(y0, y), max(x1, x), max(y1, y)
            difference[start:start + 4] = b'\xff\x00\xff\xff'
    png_write(output / 'difference.png', width, height, difference)
    return {'status': 'pass' if count == 0 else 'different', 'differentPixels': count, 'totalPixels': width * height,
            'maximumChannelDelta': maximum, 'bounds': None if not count else [x0, y0, x1, y1],
            'reference': descriptors[0], 'actual': descriptors[1], 'tolerance': 0, 'masks': []}


def report(manifest, results, assemblies=()):
    specification = json.loads(manifest.read_text(encoding='utf-8'))
    if specification.get('schemaVersion') != 1:
        raise ValueError('Unsupported gallery manifest schema.')
    checks, evidence, cases, runner_errors = {}, [], {}, []
    for file in results:
        tree = ET.parse(file)
        evidence.append({'path': str(file), 'sha256': sha(file.read_bytes())})
        for assembly in tree.findall('.//assembly'):
            errors = int(assembly.get('errors', '0'))
            details = assembly.findall('./errors/error')
            if errors < 0:
                raise ValueError('Invalid runner error count.')
            if errors or details:
                runner_errors.append({'path': str(file), 'assembly': assembly.get('name'),
                                      'count': max(errors, len(details)), 'details': [ET.tostring(error, encoding='unicode') for error in details]})
            for case in assembly.findall('.//test'):
                key = (assembly.get('name'), case.get('type'), case.get('name'))
                cases[key] = (case.get('type', ''), case.get('result', 'Unknown'))
    for name, result in cases.values():
        for identity in {name, name.split('.')[-1]}:
            checks.setdefault(identity, {}).setdefault(result, 0)
            checks[identity][result] += 1
    families = []
    for family in specification['families']:
        observed = {name: checks.get(name, {}) for name in family['checks']}
        missing = [name for name, values in observed.items() if not values]
        failed = bool(runner_errors) or any(any(result not in ('Pass', 'Skip') for result in values) for values in observed.values())
        status = 'fail' if failed else 'not-run' if missing else 'pass-with-skips' if any(values.get('Skip', 0) for values in observed.values()) else 'pass'
        families.append({**family, 'automatedStatus': status, 'missingChecks': missing, 'results': observed,
                         'nativeAcceptance': 'pending', 'limitations': family.get('limitations', []) + specification['commonLimitations']})
    inputs = []
    for item in specification['inputs']:
        file = ROOT / item['path']
        actual = sha(file.read_bytes())
        if actual.lower() != item['sha256'].lower():
            raise ValueError('Reference input changed; explicitly review/version the manifest: ' + item['path'])
        inputs.append({**item, 'verified': True})
    return {'schemaVersion': 1, 'generatedAt': datetime.now(timezone.utc).isoformat(), 'source': fingerprint(),
            'host': {'platform': platform.platform(), 'python': platform.python_version()},
            'assemblyFiles': [{'path': str(file), 'sha256': sha(file.read_bytes())} for file in assemblies],
            'reference': specification['reference'], 'inputs': inputs, 'results': evidence, 'runnerErrors': runner_errors, 'families': families,
            'resultMergePolicy': 'Within one environment, identical assembly/type/test names use the last supplied result; targeted reruns are not added to full-suite coverage.',
            'nativeAcceptance': 'pending', 'policy': 'Automated passes never imply native/reference pixel or timeline parity.'}


def main():
    parser = argparse.ArgumentParser(description='Record gallery family evidence and compare fully described lossless captures without pixel tolerances.')
    parser.add_argument('--manifest', type=Path, help='Reference input manifest for evidence reports.')
    parser.add_argument('--out', type=Path, required=True)
    parser.add_argument('--results', nargs='*', type=Path, default=[])
    parser.add_argument('--assemblies', nargs='*', type=Path, default=[])
    parser.add_argument('--compare', nargs=2, type=Path, metavar=('REFERENCE_JSON', 'ACTUAL_JSON'))
    parser.add_argument('--require-automated', action='store_true')
    args = parser.parse_args()
    if not args.compare and args.manifest is None:
        parser.error('--manifest is required for evidence reports.')
    args.out.mkdir(parents=True, exist_ok=True)
    try:
        result = compare(*args.compare, args.out) if args.compare else report(args.manifest, args.results, args.assemblies)
        (args.out / 'report.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
        if args.compare:
            return 0 if result['status'] == 'pass' else 1
        return 1 if args.require_automated and (result['runnerErrors'] or any(family['automatedStatus'] != 'pass' for family in result['families'])) else 0
    except (ValueError, OSError, ET.ParseError, KeyError, struct.error, zlib.error) as error:
        print(str(error), file=sys.stderr)
        return 2


if __name__ == '__main__':
    sys.exit(main())
