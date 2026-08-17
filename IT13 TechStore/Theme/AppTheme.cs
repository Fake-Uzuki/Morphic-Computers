using System.Drawing;

namespace IT8_TechStore.Theme
{
    /// <summary>
    /// Core Design System, Sunshine Color Palette.
    /// Derived strictly from Sunshine Yellow #F4D772 (RGB: 244, 215, 114)
    /// using the user-defined Tints, Shades, and Tones scale.
    /// </summary>
    public static class AppTheme
    {
        // ----------------------------------------------------
        // 1. TINTS SCALE (Adding white to Sunshine #F4D772)
        // ----------------------------------------------------
        public static readonly Color Tint10  = Color.FromArgb(254, 251, 241); // #FEFBF1
        public static readonly Color Tint20  = Color.FromArgb(253, 247, 227); // #FDF7E3
        public static readonly Color Tint30  = Color.FromArgb(252, 243, 213); // #FCF3D5
        public static readonly Color Tint40  = Color.FromArgb(251, 239, 199); // #FBEFC7
        public static readonly Color Tint50  = Color.FromArgb(250, 235, 185); // #FAEBB9
        public static readonly Color Tint60  = Color.FromArgb(248, 231, 170); // #F8E7AA
        public static readonly Color Tint70  = Color.FromArgb(247, 227, 156); // #F7E39C
        public static readonly Color Tint80  = Color.FromArgb(246, 223, 142); // #F6DF8E
        public static readonly Color Tint90  = Color.FromArgb(245, 219, 128); // #F5DB80
        public static readonly Color Base    = Color.FromArgb(244, 215, 114); // #F4D772 (100%)

        // ----------------------------------------------------
        // 2. SHADES SCALE (Adding black to Sunshine #F4D772)
        // ----------------------------------------------------
        public static readonly Color Shade10 = Color.FromArgb(24, 22, 11);   // #18160B
        public static readonly Color Shade20 = Color.FromArgb(49, 43, 23);   // #312B17
        public static readonly Color Shade30 = Color.FromArgb(73, 65, 34);   // #494122
        public static readonly Color Shade40 = Color.FromArgb(98, 86, 46);   // #62562E
        public static readonly Color Shade50 = Color.FromArgb(122, 108, 57); // #7A6C39
        public static readonly Color Shade60 = Color.FromArgb(146, 129, 68); // #928144
        public static readonly Color Shade70 = Color.FromArgb(171, 151, 80); // #AB9750
        public static readonly Color Shade80 = Color.FromArgb(195, 172, 91); // #C3AC5B
        public static readonly Color Shade90 = Color.FromArgb(220, 194, 103);// #DCC267

        // ----------------------------------------------------
        // 3. TONES SCALE (Adding grey to Sunshine #F4D772)
        // ----------------------------------------------------
        public static readonly Color Tone10  = Color.FromArgb(140, 137, 127);// #8C897F
        public static readonly Color Tone20  = Color.FromArgb(151, 145, 125);// #97917D
        public static readonly Color Tone30  = Color.FromArgb(163, 154, 124);// #A39A7C
        public static readonly Color Tone40  = Color.FromArgb(174, 163, 122);// #AEA37A
        public static readonly Color Tone50  = Color.FromArgb(186, 172, 121);// #BAAC79
        public static readonly Color Tone60  = Color.FromArgb(198, 180, 120);// #C6B478
        public static readonly Color Tone70  = Color.FromArgb(209, 189, 118);// #D1BD76
        public static readonly Color Tone80  = Color.FromArgb(221, 198, 117);// #DDC675
        public static readonly Color Tone90  = Color.FromArgb(232, 206, 115);// #E8CE73

        // ----------------------------------------------------
        // 4. SEMANTIC THEME MAPPINGS
        // ----------------------------------------------------
        public static readonly Color Primary             = Base;    // #F4D772 Sunshine Yellow
        public static readonly Color AppBackground        = Tint10;  // #FEFBF1 Clean Warm Background
        public static readonly Color CardBackground       = Tint20;  // #FDF7E3 Soft Card Background
        public static readonly Color CardHover            = Tint30;  // #FCF3D5 Bright Hover Tint
        public static readonly Color InputBackground      = Tint10;  // #FEFBF1 Input Fill

        public static readonly Color BorderColor          = Tone70;  // #D1BD76 Muted Border Tone
        public static readonly Color PrimaryHover         = Tint90;  // #F5DB80 Slightly lighter Sunshine hover
        public static readonly Color PrimaryPressed       = Shade90; // #DCC267 Slightly deeper Sunshine press

        public static readonly Color SidebarBg            = Shade20; // #312B17 Deep Sunshine Shade
        public static readonly Color SidebarHover         = Shade30; // #494122 Dark Sunshine Hover
        public static readonly Color SidebarSelected      = Base;    // #F4D772 Sunshine Yellow Active
        public static readonly Color SidebarSelectedText  = Shade10; // #18160B High Contrast Text
        public static readonly Color HeaderBg             = Shade10; // #18160B Darkest Shade Top Header

        public static readonly Color TextDark             = Shade10; // #18160B Primary High-Contrast Text
        public static readonly Color TextMuted            = Shade50; // #7A6C39 Subtext Tone
        public static readonly Color TextLight            = Tint20;  // #FDF7E3 Sidebar Light Text
        public static readonly Color TextLightMuted       = Tone50;  // #BAAC79 Sidebar Subtext Tone

        // ----------------------------------------------------
        // 5. TYPOGRAPHY FONTS
        // ----------------------------------------------------
        public static readonly Font HeaderFont       = new Font("Segoe UI", 16F, FontStyle.Bold);
        public static readonly Font SubheaderFont    = new Font("Segoe UI", 12F, FontStyle.Bold);
        public static readonly Font BodyFont         = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        public static readonly Font BodyBoldFont     = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        public static readonly Font SmallFont        = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        public static readonly Font StatValueFont    = new Font("Segoe UI", 20F, FontStyle.Bold);
    }
}
