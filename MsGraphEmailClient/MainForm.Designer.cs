namespace MsGraphEmailClient
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // Credential bar
        private Panel pnlCredentials;
        private Label lblTenant;
        private Label lblClient;
        private Label lblSecret;
        private Label lblEmail;
        private Label lblStatus;
        private TextBox txtTenantId;
        private TextBox txtClientId;
        private TextBox txtClientSecret;
        private TextBox txtMailboxEmail;
        private Button btnConnect;

        // Main layout
        private SplitContainer scMain;
        private SplitContainer scContent;

        // Left panel
        private ListBox lstFolders;

        // Message list + action bar
        private Panel pnlMessages;
        private ListView lvMessages;
        private ColumnHeader colFrom;
        private ColumnHeader colSubject;
        private ColumnHeader colDate;
        private Panel pnlListBar;
        private Button btnLoadMore;
        private Button btnDelete;
        private Label lblMsgCount;

        // Body preview
        private WebBrowser wbPreview;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            pnlCredentials  = new Panel();
            lblTenant       = new Label();
            lblClient       = new Label();
            lblSecret       = new Label();
            lblEmail        = new Label();
            lblStatus       = new Label();
            txtTenantId     = new TextBox();
            txtClientId     = new TextBox();
            txtClientSecret = new TextBox();
            txtMailboxEmail = new TextBox();
            btnConnect      = new Button();
            scMain          = new SplitContainer();
            lstFolders      = new ListBox();
            scContent       = new SplitContainer();
            pnlMessages     = new Panel();
            lvMessages      = new ListView();
            colFrom         = new ColumnHeader();
            colSubject      = new ColumnHeader();
            colDate         = new ColumnHeader();
            pnlListBar      = new Panel();
            btnLoadMore     = new Button();
            btnDelete       = new Button();
            lblMsgCount     = new Label();
            wbPreview       = new WebBrowser();

            pnlCredentials.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)scMain).BeginInit();
            scMain.Panel1.SuspendLayout();
            scMain.Panel2.SuspendLayout();
            scMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)scContent).BeginInit();
            scContent.Panel1.SuspendLayout();
            scContent.Panel2.SuspendLayout();
            scContent.SuspendLayout();
            pnlMessages.SuspendLayout();
            pnlListBar.SuspendLayout();
            SuspendLayout();

            // ── Credential bar ────────────────────────────────────────
            pnlCredentials.Dock   = DockStyle.Top;
            pnlCredentials.Height = 40;

            lblTenant.Text     = "Tenant ID:";
            lblTenant.AutoSize = false;
            lblTenant.SetBounds(4, 12, 58, 17);

            txtTenantId.SetBounds(64, 9, 120, 22);

            lblClient.Text     = "Client ID:";
            lblClient.AutoSize = false;
            lblClient.SetBounds(192, 12, 55, 17);

            txtClientId.SetBounds(249, 9, 120, 22);

            lblSecret.Text     = "Secret:";
            lblSecret.AutoSize = false;
            lblSecret.SetBounds(377, 12, 42, 17);

            txtClientSecret.SetBounds(421, 9, 105, 22);
            txtClientSecret.PasswordChar = '•';

            lblEmail.Text     = "Mailbox:";
            lblEmail.AutoSize = false;
            lblEmail.SetBounds(534, 12, 50, 17);

            txtMailboxEmail.SetBounds(586, 9, 155, 22);

            btnConnect.Text = "Connect";
            btnConnect.SetBounds(749, 8, 68, 24);
            btnConnect.Click += BtnConnect_Click;

            lblStatus.AutoSize  = false;
            lblStatus.SetBounds(825, 12, 380, 17);
            lblStatus.ForeColor = Color.DarkSlateBlue;

            pnlCredentials.Controls.AddRange([
                lblTenant, txtTenantId,
                lblClient, txtClientId,
                lblSecret, txtClientSecret,
                lblEmail, txtMailboxEmail,
                btnConnect, lblStatus,
            ]);

            // ── scMain: left=folders | right=content ─────────────────
            scMain.Dock        = DockStyle.Fill;
            scMain.Orientation = Orientation.Vertical;

            lstFolders.Dock        = DockStyle.Fill;
            lstFolders.BorderStyle = BorderStyle.None;
            lstFolders.SelectedIndexChanged += LstFolders_SelectedIndexChanged;
            scMain.Panel1.Controls.Add(lstFolders);

            // ── scContent: top=message list | bottom=preview ──────────
            scContent.Dock        = DockStyle.Fill;
            scContent.Orientation = Orientation.Horizontal;

            // ── pnlMessages: lvMessages + pnlListBar ──────────────────
            pnlMessages.Dock = DockStyle.Fill;

            colFrom.Text    = "From";
            colFrom.Width   = 190;
            colSubject.Text = "Subject";
            colSubject.Width = 350;
            colDate.Text    = "Date";
            colDate.Width   = 130;

            lvMessages.Dock          = DockStyle.Fill;
            lvMessages.View          = View.Details;
            lvMessages.FullRowSelect  = true;
            lvMessages.GridLines     = true;
            lvMessages.MultiSelect   = true;
            lvMessages.HideSelection = false;
            lvMessages.Columns.AddRange([colFrom, colSubject, colDate]);
            lvMessages.SelectedIndexChanged += LvMessages_SelectedIndexChanged;

            // ── pnlListBar: Load more | Delete | count ────────────────
            pnlListBar.Dock   = DockStyle.Bottom;
            pnlListBar.Height = 30;

            btnLoadMore.Text    = "Load more";
            btnLoadMore.Enabled = false;
            btnLoadMore.SetBounds(4, 3, 76, 24);
            btnLoadMore.Click += BtnLoadMore_Click;

            btnDelete.Text    = "Delete selected";
            btnDelete.Enabled = false;
            btnDelete.SetBounds(86, 3, 110, 24);
            btnDelete.Click += BtnDelete_Click;

            lblMsgCount.AutoSize  = false;
            lblMsgCount.SetBounds(204, 7, 300, 17);
            lblMsgCount.ForeColor = Color.DimGray;

            pnlListBar.Controls.AddRange([btnLoadMore, btnDelete, lblMsgCount]);

            // Fill before Bottom so docking resolves correctly
            pnlMessages.Controls.Add(lvMessages);
            pnlMessages.Controls.Add(pnlListBar);

            scContent.Panel1.Controls.Add(pnlMessages);

            wbPreview.Dock                           = DockStyle.Fill;
            wbPreview.ScriptErrorsSuppressed         = true;
            wbPreview.IsWebBrowserContextMenuEnabled = false;
            scContent.Panel2.Controls.Add(wbPreview);

            scMain.Panel2.Controls.Add(scContent);

            // ── Resume layouts ────────────────────────────────────────
            pnlListBar.ResumeLayout(false);
            pnlMessages.ResumeLayout(false);

            scContent.Panel1.ResumeLayout(false);
            scContent.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)scContent).EndInit();
            scContent.ResumeLayout(false);

            scMain.Panel1.ResumeLayout(false);
            scMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)scMain).EndInit();
            scMain.ResumeLayout(false);

            pnlCredentials.ResumeLayout(false);

            // ── Form ─────────────────────────────────────────────────
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize    = new Size(1200, 700);
            MinimumSize   = new Size(920, 500);
            Text          = "MS Graph Email Client";
            Controls.Add(scMain);
            Controls.Add(pnlCredentials);

            ResumeLayout(false);
        }
    }
}
