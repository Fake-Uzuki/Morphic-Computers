using System.Drawing;

namespace ERP.winforms.Theme
{
    public static class AppTheme
    {
        // Dark Header & Navigation Bar Tokens
        public static readonly Color HeaderBg = Color.FromArgb(20, 21, 17);              // #141511 (Dark Charcoal)
        public static readonly Color HeaderBrandGold = Color.FromArgb(245, 206, 68);     // #F5CE44
        public static readonly Color NavTabInactive = Color.Transparent;
        public static readonly Color NavTabInactiveText = Color.FromArgb(210, 212, 202); // #D2D4CA
        public static readonly Color NavTabHover = Color.FromArgb(38, 40, 32);
        public static readonly Color NavTabActive = Color.FromArgb(245, 206, 68);        // #F5CE44 (Gold)
        public static readonly Color NavTabActiveText = Color.FromArgb(22, 23, 19);       // Dark text on gold tab

        // Operations & Status Sub-Bars
        public static readonly Color OperationsBarBg = Color.FromArgb(250, 248, 243);    // #FAF8F3 (Warm Ivory)
        public static readonly Color OperationsBarBorder = Color.FromArgb(227, 223, 213);
        public static readonly Color OperationsBarText = Color.FromArgb(85, 82, 75);
        public static readonly Color BottomBarBg = Color.FromArgb(20, 21, 17);
        public static readonly Color BottomBarText = Color.FromArgb(160, 158, 148);

        // Content Canvas & Cards
        public static readonly Color AppBackground = Color.FromArgb(247, 245, 238);      // #F7F5EE (Canvas)
        public static readonly Color CardBackground = Color.White;
        public static readonly Color CardBorder = Color.FromArgb(226, 221, 208);         // #E2DDD0
        public static readonly Color CardSubtleBg = Color.FromArgb(252, 250, 245);
        public static readonly Color BorderColor = CardBorder;

        // Primary Brand Yellow / Gold Accent
        public static readonly Color Primary = Color.FromArgb(245, 206, 68);             // #F5CE44
        public static readonly Color PrimaryHover = Color.FromArgb(250, 218, 95);
        public static readonly Color PrimaryPressed = Color.FromArgb(225, 186, 50);

        // Core Text Tokens
        public static readonly Color TextDark = Color.FromArgb(22, 23, 19);              // #161713
        public static readonly Color TextMuted = Color.FromArgb(120, 118, 110);          // Neutral Grey
        public static readonly Color TextSubtle = Color.FromArgb(160, 158, 150);
        public static readonly Color TextLight = Color.FromArgb(250, 248, 243);
        public static readonly Color TextLightMuted = Color.FromArgb(160, 158, 148);

        // Status & Pill Badges
        public static readonly Color GreenPillBg = Color.FromArgb(232, 247, 240);        // In Stock / Online
        public static readonly Color GreenPillText = Color.FromArgb(27, 122, 79);
        public static readonly Color AmberPillBg = Color.FromArgb(254, 247, 230);        // Low Stock / Warning
        public static readonly Color AmberPillText = Color.FromArgb(168, 110, 5);
        public static readonly Color RedPillBg = Color.FromArgb(253, 236, 235);          // Critical / Out of Stock
        public static readonly Color RedPillText = Color.FromArgb(184, 50, 38);
        public static readonly Color BluePillBg = Color.FromArgb(235, 243, 253);         // Info / Regular
        public static readonly Color BluePillText = Color.FromArgb(30, 95, 180);
        public static readonly Color GoldPillBg = Color.FromArgb(254, 245, 215);
        public static readonly Color GoldPillText = Color.FromArgb(130, 95, 10);

        // DataGrid Theming (Light Gold Header Row matching screenshot)
        public static readonly Color GridHeaderBg = Color.FromArgb(246, 231, 184);       // #F6E7B8 (Screenshot Grid Header)
        public static readonly Color GridHeaderBorder = Color.FromArgb(220, 202, 146);
        public static readonly Color GridHeaderText = Color.FromArgb(58, 52, 35);        // #3A3423
        public static readonly Color GridRowHover = Color.FromArgb(253, 249, 238);
        public static readonly Color GridRowSelected = Color.FromArgb(250, 241, 212);
        public static readonly Color GridGridLines = Color.FromArgb(236, 232, 222);

        // Typography Hierarchy
        public static readonly Font HeaderFont = new Font("Segoe UI", 16F, FontStyle.Bold);
        public static readonly Font SubheaderFont = new Font("Segoe UI", 12F, FontStyle.Bold);
        public static readonly Font BodyFont = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        public static readonly Font BodyBoldFont = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        public static readonly Font SmallFont = new Font("Segoe UI", 8F, FontStyle.Regular);
        public static readonly Font SmallBoldFont = new Font("Segoe UI", 8F, FontStyle.Bold);
        public static readonly Font StatValueFont = new Font("Segoe UI", 20F, FontStyle.Bold);
        public static readonly Font StatHeroFont = new Font("Segoe UI", 26F, FontStyle.Bold);
    }
}
