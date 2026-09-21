using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using ERP.winforms.Theme;

namespace ERP.winforms.UI.Components
{
    public class NotificationItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Category { get; set; } = "SYSTEM"; // STOCK, APPROVAL, REPAIR, SYSTEM
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
        public string ActionText { get; set; } = "View Details →";
        public Action? OnClickAction { get; set; }
        public Action? OnDismiss { get; set; }
    }

    public class NotificationFlyout : UserControl
    {
        private readonly List<NotificationItem> _items = new();
        private Panel _pnlHeader = null!;
        private Panel _pnlList = null!;
        private Panel _pnlFooter = null!;
        private Label _lblTitle = null!;
        private Label _lblBadge = null!;
        private Button _btnMarkRead = null!;
        private Label _lblFooterCount = null!;

        public Action? OnNotificationsChanged;
        public Action? OnRequestClose;

        public int UnreadCount => _items.Count(i => !i.IsRead);

        public NotificationFlyout()
        {
            Size = new Size(390, 480);
            BackColor = Color.White;
            BorderStyle = BorderStyle.None;

            InitializeLayout();
        }

        private void InitializeLayout()
        {
            Controls.Clear();

            // 1. Header (Dark Charcoal to match main app header)
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = AppTheme.HeaderBg,
                Padding = new Padding(14, 0, 14, 0)
            };

            Label lblBell = new Label
            {
                Text = "🔔",
                Font = new Font("Segoe UI Emoji", 11F, FontStyle.Regular),
                ForeColor = AppTheme.Primary,
                Location = new Point(12, 14),
                Size = new Size(24, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblTitle = new Label
            {
                Text = "Notifications & Alerts",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(38, 14),
                AutoSize = true
            };

            _lblBadge = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                BackColor = AppTheme.Primary,
                Location = new Point(190, 14),
                Size = new Size(22, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _btnMarkRead = new Button
            {
                Text = "Clear All",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = AppTheme.NavTabInactiveText,
                BackColor = Color.FromArgb(32, 34, 28),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(80, 24),
                Location = new Point(295, 12),
                Cursor = Cursors.Hand
            };
            _btnMarkRead.FlatAppearance.BorderSize = 0;
            _btnMarkRead.Click += (s, e) => MarkAllAsRead();

            _pnlHeader.Controls.Add(lblBell);
            _pnlHeader.Controls.Add(_lblTitle);
            _pnlHeader.Controls.Add(_lblBadge);
            _pnlHeader.Controls.Add(_btnMarkRead);

            // 2. Footer
            _pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.FromArgb(248, 246, 240),
                Padding = new Padding(12, 6, 12, 6)
            };

            _lblFooterCount = new Label
            {
                Text = "0 active alerts",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true
            };

            Button btnClose = new Button
            {
                Text = "Close ✕",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right,
                Width = 65,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => OnRequestClose?.Invoke();

            _pnlFooter.Controls.Add(_lblFooterCount);
            _pnlFooter.Controls.Add(btnClose);

            // 3. Scrollable Notifications List
            _pnlList = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(6)
            };

            Controls.Add(_pnlList);
            Controls.Add(_pnlFooter);
            Controls.Add(_pnlHeader);

            Paint += (s, e) =>
            {
                using Pen borderPen = new Pen(Color.FromArgb(190, 185, 170), 1);
                e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            };
        }

        public void SetNotifications(List<NotificationItem> items)
        {
            _items.Clear();
            _items.AddRange(items);
            RefreshUI();
        }

        public void AddNotification(NotificationItem item)
        {
            _items.Insert(0, item);
            RefreshUI();
        }

        public void DismissNotification(NotificationItem item)
        {
            item.OnDismiss?.Invoke();
            _items.Remove(item);
            RefreshUI();
            OnNotificationsChanged?.Invoke();
        }

        public void MarkAllAsRead()
        {
            foreach (var item in _items.ToList())
            {
                item.OnDismiss?.Invoke();
            }
            _items.Clear();
            RefreshUI();
            OnNotificationsChanged?.Invoke();
        }

        public void RefreshUI()
        {
            int unread = UnreadCount;
            _lblBadge.Text = unread.ToString();
            _lblBadge.Visible = unread > 0;
            _lblBadge.Location = new Point(_lblTitle.Right + 6, 14);

            _lblFooterCount.Text = $"{_items.Count} alert{(_items.Count == 1 ? "" : "s")} active";

            _pnlList.SuspendLayout();
            _pnlList.Controls.Clear();

            if (_items.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = "✓ All Caught Up!\nNo active system alerts or notifications.",
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    ForeColor = AppTheme.TextMuted,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                _pnlList.Controls.Add(lblEmpty);
            }
            else
            {
                for (int i = _items.Count - 1; i >= 0; i--)
                {
                    var item = _items[i];
                    Panel card = CreateNotificationCard(item);
                    _pnlList.Controls.Add(card);
                }
            }

            _pnlList.ResumeLayout();
        }

        private Panel CreateNotificationCard(NotificationItem item)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                Margin = new Padding(0, 0, 0, 6),
                Padding = new Padding(10, 8, 10, 8),
                BackColor = item.IsRead ? Color.FromArgb(252, 251, 248) : Color.FromArgb(255, 252, 235),
                Cursor = Cursors.Hand
            };

            card.Paint += (s, e) =>
            {
                using Pen borderPen = new Pen(item.IsRead ? Color.FromArgb(235, 232, 222) : AppTheme.Primary, 1);
                e.Graphics.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);

                if (!item.IsRead)
                {
                    using Brush accentBrush = new SolidBrush(AppTheme.Primary);
                    e.Graphics.FillRectangle(accentBrush, 0, 0, 4, card.Height);
                }
            };

            Color catBg = item.Category switch
            {
                "STOCK" => AppTheme.RedPillBg,
                "APPROVAL" => AppTheme.BluePillBg,
                "REPAIR" => AppTheme.AmberPillBg,
                _ => AppTheme.GreenPillBg
            };

            Color catText = item.Category switch
            {
                "STOCK" => AppTheme.RedPillText,
                "APPROVAL" => AppTheme.BluePillText,
                "REPAIR" => AppTheme.AmberPillText,
                _ => AppTheme.GreenPillText
            };

            string catLabel = item.Category switch
            {
                "STOCK" => "STOCK ALERT",
                "APPROVAL" => "APPROVAL REQ",
                "REPAIR" => "REPAIR TICKET",
                _ => "SYSTEM"
            };

            Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 20, BackColor = Color.Transparent };

            Panel pnlPill = new Panel
            {
                Dock = DockStyle.Left,
                Width = 84,
                BackColor = catBg,
                Margin = new Padding(0)
            };
            Label lblPill = new Label
            {
                Text = catLabel,
                Font = new Font("Segoe UI", 6.8F, FontStyle.Bold),
                ForeColor = catText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlPill.Controls.Add(lblPill);

            // Close / Dismiss 'X' button
            Label lblDismiss = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Right,
                Width = 18,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            lblDismiss.Click += (s, e) => DismissNotification(item);
            lblDismiss.MouseEnter += (s, e) => lblDismiss.ForeColor = Color.Red;
            lblDismiss.MouseLeave += (s, e) => lblDismiss.ForeColor = AppTheme.TextMuted;

            string timeText = GetRelativeTime(item.Timestamp);
            Label lblTime = new Label
            {
                Text = timeText,
                Font = new Font("Segoe UI", 7F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true
            };

            pnlTop.Controls.Add(pnlPill);
            pnlTop.Controls.Add(lblTime);
            pnlTop.Controls.Add(lblDismiss);

            Label lblTitle = new Label
            {
                Text = item.Title,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = AppTheme.TextDark,
                Dock = DockStyle.Top,
                Height = 18,
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };

            Label lblMessage = new Label
            {
                Text = item.Message,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Regular),
                ForeColor = AppTheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 16,
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };

            Label lblAction = new Label
            {
                Text = item.ActionText,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(145, 105, 15),
                Dock = DockStyle.Bottom,
                Height = 16,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };

            card.Controls.Add(lblAction);
            card.Controls.Add(lblMessage);
            card.Controls.Add(lblTitle);
            card.Controls.Add(pnlTop);

            void HandleClick(object? sender, EventArgs e)
            {
                DismissNotification(item);
                OnRequestClose?.Invoke();
                item.OnClickAction?.Invoke();
            }

            card.Click += HandleClick;
            lblTitle.Click += HandleClick;
            lblMessage.Click += HandleClick;
            lblAction.Click += HandleClick;

            return card;
        }

        private static string GetRelativeTime(DateTime dt)
        {
            var span = DateTime.Now - dt;
            if (span.TotalMinutes < 1) return "Just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            return dt.ToString("MMM dd");
        }
    }
}
