using Nazor.Rendering;
using Nazor.Style;

namespace Nazor.DevTools.Fixtures;

public static class InspectionStyles
{
    private static Selector PieceClass => new([new(null, null, ["piece"], ElementStates.None)], []);
    public static StyleSheet Theme() => new("theme.nss", [new(PieceClass, null,
        [new ValueDeclaration(StyleProperties.Color, new Color(0, 0, 255, 255)), new ValueDeclaration(StyleProperties.PaddingTop, Length.Px(3))], 0)]);
    public static StyleSheet Application() => new("app.nss", [
        new(new Selector([new("Piece", null, [], ElementStates.None)], []), null,
            [new ValueDeclaration(StyleProperties.Color, new Color(0, 128, 0, 255))], 0),
        new(PieceClass, null, [new ValueDeclaration(StyleProperties.PaddingTop, Length.Px(4))], 1),
        new(PieceClass, new MediaQuery([new MediaCondition(9999, null, null, null)]), [new ValueDeclaration(StyleProperties.PaddingTop, Length.Px(99))], 2)
    ]);
}
