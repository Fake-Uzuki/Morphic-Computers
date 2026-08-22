using System.Drawing;

namespace ERP.winforms.Theme
{
    public static class AppTheme
    {
        // Base Sunshine Yellow (#F4D772) Palette Tokens
        public static readonly Color BaseYellow = Color.FromArgb(244, 215, 114);

        // Tints (Light Backgrounds)
        public static readonly Color AppBackground = Color.FromArgb(254, 251, 241); // 10% Tint (#FEFBF1)
        public static readonly Color CardBackground = Color.FromArgb(253, 247, 227); // 20% Tint (#FDF7E3)
        public static readonly Color CardHover = Color.FromArgb(252, 243, 213);      // 30% Tint (#FCF3D5)
        public static readonly Color PrimaryHover = Color.FromArgb(245, 219, 128);  // 90% Tint (#F5DB80)

        // Primary Accent
        public static readonly Color Primary = BaseYellow;                           // Base (#F4D772)
        public static readonly Color PrimaryPressed = Color.FromArgb(220, 194, 103);// 90% Shade (#DCC267)

        // Shades (Dark Backgrounds & Text)
        public static readonly Color TextDark = Color.FromArgb(24, 22, 11);         // 10% Shade (#18160B)
        public static readonly Color HeaderBg = Color.FromArgb(24, 22, 11);         // Dark Header
        public static readonly Color SidebarBg = Color.FromArgb(49, 43, 23);        // 20% Shade (#312B17)
        public static readonly Color SidebarHover = Color.FromArgb(73, 65, 34);     // 30% Shade (#494122)

        // Selection & Accents
        public static readonly Color SidebarSelected = Primary;                      // Bright Yellow Active Pill
        public static readonly Color SidebarSelectedText = TextDark;                 // High Contrast Dark Text
        public static readonly Color TextLight = Color.FromArgb(254, 251, 241);     // Crisp White/Light
        public static readonly Color TextLightMuted = Color.FromArgb(200, 190, 150); // Muted Light Text
        public static readonly Color TextMuted = Color.FromArgb(122, 108, 57);      // Muted Gold Text

        // Tones (Borders)
        public static readonly Color BorderColor = Color.FromArgb(209, 189, 118);    // 70% Tone (#D1BD76)

        // Fonts
        public static readonly Font HeaderFont = new Font("Segoe UI", 18F, FontStyle.Bold);
        public static readonly Font SubheaderFont = new Font("Segoe UI", 14F, FontStyle.Bold);
        public static readonly Font BodyFont = new Font("Segoe UI", 10F, FontStyle.Regular);
        public static readonly Font BodyBoldFont = new Font("Segoe UI", 10F, FontStyle.Bold);
        public static readonly Font SmallFont = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        public static readonly Font StatValueFont = new Font("Segoe UI", 22F, FontStyle.Bold);
    }
}
