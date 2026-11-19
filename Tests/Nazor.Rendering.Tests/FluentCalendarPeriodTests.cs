using System.Globalization;
using Nazor.Controls;
using Nazor.Graphics;
using Nazor.Input;
using Nazor.Style;
using Nazor.Testing;
using Calendar=Nazor.Controls.Calendar;

namespace Nazor.Rendering.Tests;

public sealed class FluentCalendarPeriodTests
{
    private static void Configure(RenderHarness h)
    {
        h.Scene.Styles.Theme=Nazor.Themes.Fluent.Themes.Fluent;
        foreach(var file in Nazor.Themes.Fluent.Themes.FontFiles) ((Nazor.Skia.SkiaFontProvider)h.Graphics.Fonts).Register(Path.Combine(AppContext.BaseDirectory,file));
    }

    [Theory]
    [InlineData(1,false,1f)] [InlineData(1,false,1.5f)] [InlineData(1,false,2f)]
    [InlineData(1,true,1f)] [InlineData(2,false,1f)] [InlineData(2,true,1f)]
    public async Task Period_views_preserve_month_geometry_and_use_circular_selected_items(int months,bool weeks,float scale)
    {
        using var h=new RenderHarness();Configure(h);
        var parameters=new Dictionary<string,object?>
        {
            [nameof(Calendar.Culture)]=CultureInfo.GetCultureInfo("en-US"),[nameof(Calendar.DisplayMonth)]=new DateOnly(2026,11,1),
            [nameof(Calendar.Value)]=new DateOnly(2026,11,15),[nameof(Calendar.Today)]=new DateOnly(2026,11,12),
            [nameof(Calendar.MonthCount)]=months,[nameof(Calendar.ShowWeekNumbers)]=weeks,
        };
        var root=h.Host.Render<Calendar>(ParameterView.FromDictionary(parameters));Frame();
        var month=h.Scene.GetBounds(root.Single("Calendar"));
        var titleHeight=root.Elements("CalendarMonthTitle").Select(h.Scene.GetBounds).FirstOrDefault().Height;
        foreach(var view in new[]{CalendarViewMode.Year,CalendarViewMode.Decade})
        {
            parameters[nameof(Calendar.View)]=view;
            await root.Component.SetParametersAsync(ParameterView.FromDictionary(parameters));Frame();
            var actual=h.Scene.GetBounds(root.Single("Calendar"));
            Assert.InRange(Math.Abs(actual.Width-month.Width),0,1/scale);
            Assert.True(Math.Abs(actual.Height-month.Height)<=1/scale,$"Month {month}; period {actual}; month title height {titleHeight}.");
            var selected=Assert.Single(root.Elements("CalendarItem"),item=>item.Attributes.GetValueOrDefault("selected") is true);
            var bounds=h.Scene.GetBounds(selected);var style=h.Scene.GetStyle(selected);
            Assert.Equal(56f,bounds.Width,3);Assert.Equal(bounds.Width,bounds.Height,3);
            Assert.Equal(Length.Px(999),style.Get(StyleProperties.BorderTopLeftRadius).X);
            Assert.NotEqual(0,style.BackgroundColor.A);
        }
        void Frame(){h.Host.RunPending();h.Render(1000,600,scale);h.Host.RunPending();h.Render(1000,600,scale);}
    }

    [Theory]
    [InlineData(ColorScheme.Light,false)] [InlineData(ColorScheme.Dark,false)] [InlineData(ColorScheme.Dark,true)]
    public void Date_picker_zoom_keeps_its_popup_size_and_selected_circle(ColorScheme scheme,bool forced)
    {
        using var h=new RenderHarness();using var input=new InputManager(h.Host.Renderer,h.Scene);
        Configure(h);
        h.Scene.Styles.Preferences=new MediaPreferences(scheme,ReducedMotion:true,ForcedColors:forced,Colors:SystemColors.Default);
        var root=h.Host.Render<DatePicker>(ParameterView.FromDictionary(new Dictionary<string,object?>
        {
            [nameof(DatePicker.Value)]=new DateOnly(2026,11,15),[nameof(DatePicker.Today)]=new DateOnly(2026,11,12),
            [nameof(Control.Style)]="width:200px; margin:20px;",
        }));Frame();Click(root.Single("DatePickerButton"));
        var month=h.Scene.GetBounds(root.Single("DatePickerPopup"));
        for(var step=0;step<2;step++)
        {
            Click(root.Single("CalendarTitle"));
            var actual=h.Scene.GetBounds(root.Single("DatePickerPopup"));
            Assert.Equal(month.Width,actual.Width);Assert.Equal(month.Height,actual.Height);
            var selected=Assert.Single(root.Elements("CalendarItem"),item=>item.Attributes.GetValueOrDefault("selected") is true);
            var bounds=h.Scene.GetBounds(selected);Assert.Equal(bounds.Width,bounds.Height);
            var style=h.Scene.GetStyle(selected);Assert.NotEqual(0,style.BackgroundColor.A);
            if(forced) {Assert.Equal(SystemColors.Default.Highlight,style.BackgroundColor);Assert.Equal(SystemColors.Default.HighlightText,style.Color);}
        }
        var image=h.Render(460,480,1);
        var folder=Path.Combine(AppContext.BaseDirectory,"artifacts","fluent-polish");Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder,$"calendar-period-{scheme}-{forced}.png"),h.Graphics.EncodePng(image));
        void Frame(){h.Host.RunPending();h.Render(460,480,1);h.Host.RunPending();h.Render(460,480,1);}
        void Click(ElementNode node){var b=h.Scene.GetBounds(node);var point=new Point(b.X+b.Width/2,b.Y+b.Height/2);input.PointerPressed(point,PointerButton.Primary,KeyModifiers.None);input.PointerReleased(point,PointerButton.Primary,KeyModifiers.None);Frame();}
    }
}
