namespace MsGraphEmailClient
{
    public partial class MainForm : Form
    {
        private GraphService? _graphService;
        private readonly List<string> _folderIds  = [];
        private readonly List<string> _messageIds = [];
        private string? _nextLink;

        public MainForm()
        {
            InitializeComponent();

            Shown += (_, _) =>
            {
                scMain.SplitterDistance = 200;
                scMain.Panel1MinSize    = 140;
                scMain.Panel2MinSize    = 300;

                scContent.SplitterDistance = 220;
                scContent.Panel1MinSize    = 80;
                scContent.Panel2MinSize    = 80;
            };

            // Auto-load next page when the user scrolls to the last visible message
            lvMessages.MouseWheel += async (_, _) =>
            {
                if (!btnLoadMore.Enabled || lvMessages.Items.Count == 0) return;
                var last = lvMessages.Items[^1];
                if (lvMessages.ClientRectangle.Bottom >= last.Bounds.Bottom)
                    await LoadMoreAsync();
            };
        }

        // ── Connect ───────────────────────────────────────────────────

        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTenantId.Text)    ||
                string.IsNullOrWhiteSpace(txtClientId.Text)    ||
                string.IsNullOrWhiteSpace(txtClientSecret.Text) ||
                string.IsNullOrWhiteSpace(txtMailboxEmail.Text))
            {
                SetStatus("All credential fields are required.", Color.Crimson);
                return;
            }

            btnConnect.Enabled = false;
            lstFolders.Items.Clear();
            _folderIds.Clear();
            ClearMessages();
            SetStatus("Connecting…", Color.DarkSlateBlue);

            try
            {
                _graphService = new GraphService(
                    txtTenantId.Text.Trim(),
                    txtClientId.Text.Trim(),
                    txtClientSecret.Text,
                    txtMailboxEmail.Text.Trim());

                var folders = await _graphService.GetFoldersAsync();
                foreach (var f in folders)
                {
                    lstFolders.Items.Add(f.DisplayName);
                    _folderIds.Add(f.Id);
                }
                SetStatus($"Connected. {folders.Count} folder(s) loaded.", Color.DarkGreen);
            }
            catch (Exception ex)
            {
                _graphService = null;
                SetStatus($"Error: {ex.Message}", Color.Crimson);
            }
            finally
            {
                btnConnect.Enabled = true;
            }
        }

        // ── Folder selection ──────────────────────────────────────────

        private async void LstFolders_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = lstFolders.SelectedIndex;
            if (idx < 0 || _graphService is null) return;

            ClearMessages();
            SetStatus("Loading messages…", Color.DarkSlateBlue);

            try
            {
                var page = await _graphService.GetMessagesAsync(_folderIds[idx]);
                AppendMessages(page.Messages);
                _nextLink          = page.NextLink;
                btnLoadMore.Enabled = _nextLink is not null;
                SetStatus($"{lvMessages.Items.Count} message(s).", Color.DarkGreen);
            }
            catch (Exception ex)
            {
                SetStatus($"Error loading messages: {ex.Message}", Color.Crimson);
            }
        }

        // ── Load more ─────────────────────────────────────────────────

        private async void BtnLoadMore_Click(object sender, EventArgs e) => await LoadMoreAsync();

        private async Task LoadMoreAsync()
        {
            if (_nextLink is null || _graphService is null) return;

            btnLoadMore.Enabled = false;
            SetStatus("Loading more…", Color.DarkSlateBlue);

            try
            {
                var page = await _graphService.GetMoreMessagesAsync(_nextLink);
                AppendMessages(page.Messages);
                _nextLink           = page.NextLink;
                btnLoadMore.Enabled = _nextLink is not null;
                SetStatus($"{lvMessages.Items.Count} message(s).", Color.DarkGreen);
            }
            catch (Exception ex)
            {
                btnLoadMore.Enabled = _nextLink is not null;
                SetStatus($"Error loading more: {ex.Message}", Color.Crimson);
            }
        }

        // ── Delete selected ───────────────────────────────────────────

        private async void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_graphService is null || lvMessages.SelectedItems.Count == 0) return;

            int count = lvMessages.SelectedItems.Count;
            if (MessageBox.Show($"Delete {count} message(s)?", "Confirm delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            // Snapshot index→id pairs; highest indices first so removals don't shift lower ones
            var toDelete = lvMessages.SelectedIndices.Cast<int>()
                .Select(i => (Index: i, Id: _messageIds[i]))
                .OrderByDescending(x => x.Index)
                .ToList();

            btnDelete.Enabled      = false;
            btnLoadMore.Enabled    = false;
            wbPreview.DocumentText = string.Empty;
            SetStatus($"Deleting {count} message(s)…", Color.DarkSlateBlue);

            lvMessages.SelectedIndexChanged -= LvMessages_SelectedIndexChanged;
            try
            {
                var progress = new Progress<int>(n =>
                    SetStatus($"Deleted {n} of {count}…", Color.DarkSlateBlue));
                var deletedIds = await _graphService.DeleteMessagesAsync(
                    toDelete.Select(x => x.Id).ToList(), progress);

                foreach (var (index, id) in toDelete)
                {
                    if (!deletedIds.Contains(id)) continue;
                    lvMessages.Items.RemoveAt(index);
                    _messageIds.RemoveAt(index);
                }

                int failed = count - deletedIds.Count;
                UpdateMsgCount();
                SetStatus(
                    failed == 0
                        ? $"Deleted {deletedIds.Count} message(s)."
                        : $"Deleted {deletedIds.Count}, {failed} failed.",
                    failed == 0 ? Color.DarkGreen : Color.DarkOrange);
            }
            catch (Exception ex)
            {
                SetStatus($"Delete error: {ex.Message}", Color.Crimson);
            }
            finally
            {
                lvMessages.SelectedIndexChanged += LvMessages_SelectedIndexChanged;
                btnDelete.Enabled   = lvMessages.SelectedItems.Count > 0;
                btnLoadMore.Enabled = _nextLink is not null;
            }
        }

        // ── Message selection / preview ───────────────────────────────

        private async void LvMessages_SelectedIndexChanged(object? sender, EventArgs e)
        {
            btnDelete.Enabled = lvMessages.SelectedItems.Count > 0;
            UpdateMsgCount();

            // Preview the focused (last-clicked) message, not just SelectedIndices[0]
            var focused = lvMessages.FocusedItem;
            if (focused is null || _graphService is null) return;

            int idx = focused.Index;
            wbPreview.DocumentText = "<p>Loading…</p>";
            SetStatus("Loading message…", Color.DarkSlateBlue);

            try
            {
                string html = await _graphService.GetMessageBodyAsync(_messageIds[idx]);
                wbPreview.DocumentText = html;
                SetStatus(string.Empty, Color.DarkSlateBlue);
            }
            catch (Exception ex)
            {
                wbPreview.DocumentText =
                    $"<p style='color:red'>Error: {System.Net.WebUtility.HtmlEncode(ex.Message)}</p>";
                SetStatus($"Error loading body: {ex.Message}", Color.Crimson);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────

        private void AppendMessages(List<MessageSummary> messages)
        {
            foreach (var msg in messages)
            {
                string date = msg.ReceivedDateTime?.LocalDateTime.ToString("g") ?? string.Empty;
                var item = new ListViewItem(msg.From);
                item.SubItems.Add(msg.Subject);
                item.SubItems.Add(date);
                if (!msg.IsRead)
                    item.Font = new Font(lvMessages.Font, FontStyle.Bold);
                lvMessages.Items.Add(item);
                _messageIds.Add(msg.Id);
            }
            UpdateMsgCount();
        }

        private void ClearMessages()
        {
            lvMessages.Items.Clear();
            _messageIds.Clear();
            _nextLink           = null;
            btnLoadMore.Enabled = false;
            btnDelete.Enabled   = false;
            lblMsgCount.Text    = string.Empty;
            wbPreview.DocumentText = string.Empty;
        }

        private void UpdateMsgCount()
        {
            int total    = lvMessages.Items.Count;
            int selected = lvMessages.SelectedItems.Count;
            lblMsgCount.Text = selected > 0
                ? $"{total} messages, {selected} selected"
                : $"{total} messages";
        }

        private void SetStatus(string message, Color color)
        {
            lblStatus.Text      = message;
            lblStatus.ForeColor = color;
        }
    }
}
