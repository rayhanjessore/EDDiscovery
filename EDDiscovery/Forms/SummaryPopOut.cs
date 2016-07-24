﻿using EDDiscovery.DB;
using EDDiscovery2.DB;
using EDDiscovery2.EDSM;
using ExtendedControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EDDiscovery2
{
    public partial class SummaryPopOut : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        public const int WM_NCL_RESIZE = 0x112;
        public const int HT_RESIZE = 61448;

        private IntPtr SendMessage(int msg, IntPtr wparam, IntPtr lparam)
        {
            Message message = Message.Create(this.Handle, msg, wparam, lparam);
            this.WndProc(ref message);
            return message.Result;
        }

        private Color transparentkey = Color.Red;
        private Timer autofade = new Timer();
        private ControlTable lt;
        private List<int> tabstops = new List<int>();
        private Font butfont = new Font("Microsoft Sans Serif", 8.25F);
        private int statictoplines = 0;
        private bool buttons;
        private bool noteafterxyz;

        public SummaryPopOut( bool b , bool n )
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            InitializeComponent();
            panel_grip.Visible = false;
            autofade.Interval = 500;
            autofade.Tick += FadeOut;
            TopMost = true;
            lt = new ControlTable(this, 30, 8, 20);
            UpdateEventsOnControls(this);
            this.BackColor = transparentkey;
            this.TransparencyKey = transparentkey;

            buttons = b;
            noteafterxyz = n;

            if (buttons)
                tabstops.AddRange(new int[] { 4, 44 });             // button, time
            else
                tabstops.AddRange(new int[] { 4 });                 // time

            int lastpos = tabstops[tabstops.Count - 1];

            tabstops.AddRange(new int[] { lastpos + 50, lastpos + 200 });    // dist, sys
            lastpos = tabstops[tabstops.Count - 1];

            if ( noteafterxyz )
                tabstops.AddRange(new int[] { lastpos + 60, lastpos + 110 , lastpos+160, lastpos+210, 1000 });    // x,y,z,note,end
            else
                tabstops.AddRange(new int[] { lastpos + 60, lastpos + 210, lastpos+260, lastpos+310, 1000 });    // note,x,y,z,end
        }

        public void SetGripperColour(Color grip)
        {
            if (grip.GetBrightness() < 0.15)       // override if its too dark..
                grip = Color.Orange;

            labelExt_NoSystems.ForeColor = grip;

            panel_grip.ForeColor = grip;
            panel_grip.MouseOverColor = ButtonExt.Multiply(grip, 1.3F);
            panel_grip.MouseSelectedColor = ButtonExt.Multiply(grip, 1.5F);

            transparentkey = (grip == Color.Red) ? Color.Green : Color.Red;
            this.BackColor = transparentkey;
            this.TransparencyKey = transparentkey;
        }

        public void ResetForm(DataGridView vsc)
        {
            if (vsc != null)
            {
                SuspendLayout();
                lt.Dispose();
                statictoplines = 0;

                for (int i = 0; i < vsc.Rows.Count; i++)
                {
                    UpdateRow(vsc, vsc.Rows[i], true, 200000);     // insert at end, give it a large number
                    if (lt.Full)
                        break;
                }

                labelExt_NoSystems.Visible = (lt.Count == statictoplines);
                ResumeLayout();
            }
            UpdateEventsOnControls(this);
        }

        public void RefreshTarget(DataGridView vsc, List<VisitedSystemsClass> vscl)
        {
            if (vsc != null)
            {
                SuspendLayout();

                string name;
                double x, y, z;
                bool targetpresent = TargetClass.GetTargetPosition(out name, out x, out y, out z);

                if (targetpresent)
                {
                    List<string> lab = new List<string>();

                    if (buttons)
                        lab.Add("");
              
                    SystemClass cs = VisitedSystemsClass.GetSystemClassFirstPosition(vscl);

                    if (cs != null)
                        lab.AddRange(new string[] { "Target", name, SystemClass.Distance(cs, x, y, z).ToString("0.00"), "" });
                    else
                        lab.AddRange(new string[] { "Target", name, "Unknown", "" });

                    if (statictoplines == 0)
                    {
                        int row = lt.Add(lab, 0, -1);       // insert at 0  with row id of -1

                        List<Font> fnt = new List<Font>();
                        List<Color> cols = new List<Color>();

                        if (buttons)
                        {
                            fnt.Add(butfont);
                            cols.Add(panel_grip.ForeColor);
                        }

                        cols.AddRange(new Color[] { panel_grip.ForeColor, panel_grip.ForeColor, panel_grip.ForeColor, panel_grip.ForeColor });

                        fnt.AddRange(new Font[] { FontSel(vsc.Columns[1].DefaultCellStyle.Font,vsc.Font) ,
                                                    FontSel(vsc.Columns[1].DefaultCellStyle.Font,vsc.Font) ,
                                                    FontSel(vsc.Columns[2].DefaultCellStyle.Font,vsc.Font) ,
                                                    FontSel(vsc.Columns[3].DefaultCellStyle.Font,vsc.Font) });
                        FormatRow(0, fnt, cols);
                        statictoplines = 1;
                        UpdateEventsOnControls(this);
                    }
                    else
                        lt.Refresh(lab, -1);     // just refresh data, with a row ID of -1
                }
                else
                {
                    if (statictoplines == 1)
                    {
                        lt.RemoveAt(0);
                        statictoplines = 0;
                    }
                }

                labelExt_NoSystems.Visible = (lt.Count == statictoplines);
                ResumeLayout();
            }
        }


        private static Font FontSel(Font f, Font g) { return (f != null) ? f : g; }
        private static Color CSel(Color f, Color g) { return (f.A != 0) ? f : g; }

        public void RefreshRow(DataGridView vsc, DataGridViewRow vscrow, bool addattop = false )
        {
            SuspendLayout();
            UpdateRow(vsc, vscrow, addattop , statictoplines );      // add= 0 , we do a refesh.  If all=true, we do an insert at top
            UpdateEventsOnControls(this);
            ResumeLayout();
        }

        private void UpdateRow(DataGridView vsc, DataGridViewRow vscrow, bool addit , int insertat )
        {
            Debug.Assert(vscrow != null && vsc != null);

            if (!vscrow.Visible)            // may not be visible due to being turned off.. if so, reject.
                return;

            List<string> lab = new List<string>();

            if (buttons)
                lab.Add("!!<EDSMBUT:" + (string)vscrow.Cells[1].Value);

            lab.AddRange(new string[] { ((DateTime)vscrow.Cells[0].Value).ToString("HH:mm.ss") ,
                                              (string)vscrow.Cells[1].Value,
                                                (string)vscrow.Cells[2].Value });

            VisitedSystemsClass vscentry = (VisitedSystemsClass)vscrow.Cells[EDDiscovery.TravelHistoryControl.TravelHistoryColumns.SystemName].Tag;

            if (!noteafterxyz)
                lab.Add((string)vscrow.Cells[3].Value);
            if ( vscentry!= null)       // double check
                lab.AddRange(new string[] { vscentry.X.ToString("0.00"), vscentry.Y.ToString("0.00"), vscentry.Z.ToString("0.00") });
            if (noteafterxyz)
                lab.Add((string)vscrow.Cells[3].Value);

            labelExt_NoSystems.Visible = (vsc.Rows.Count == 0);

            if (addit)
            {
                int row = lt.Add(lab, insertat , vscrow.Index);        // row on the summary screen..  remember which row it came from

                List<Font> fnt = new List<Font>();
                List<Color> cols = new List<Color>();

                if (buttons)
                {
                    fnt.Add(butfont);
                    cols.Add(panel_grip.ForeColor);
                }

                fnt.AddRange(new Font[] {   FontSel(vsc.Columns[0].DefaultCellStyle.Font,vsc.Font) ,        // time
                                            FontSel(vsc.Columns[1].DefaultCellStyle.Font,vsc.Font) ,        // system
                                            FontSel(vsc.Columns[2].DefaultCellStyle.Font,vsc.Font) ,        // dist
                                            FontSel(vsc.Columns[ (noteafterxyz) ? 0 : 3].DefaultCellStyle.Font,vsc.Font) ,        // note or x
                                            FontSel(vsc.Columns[0].DefaultCellStyle.Font,vsc.Font),         // xyz
                                            FontSel(vsc.Columns[0].DefaultCellStyle.Font,vsc.Font),         // xyz
                                            FontSel(vsc.Columns[ (noteafterxyz) ? 3 : 0].DefaultCellStyle.Font,vsc.Font) });     //z
                
                Color rowc = CSel(vsc.Rows[vscrow.Index].DefaultCellStyle.ForeColor, vsc.ForeColor);

                if (rowc.GetBrightness() < 0.15)       // override if its too dark..
                    rowc = Color.White;

                cols.AddRange(new Color[] { rowc, rowc, rowc, rowc, rowc, rowc, rowc });

                FormatRow(row, fnt, cols);
            }
            else
            {
                lt.Refresh(lab, vscrow.Index);      // refresh this one with rowid= index
                //                Console.WriteLine("Refresh " + vscrow.Index + " " + (string)vscrow.Cells[1].Value);
            }
        }

        private void FormatRow(int row, List<Font> fnt, List<Color> cols)       // row on screen..
        {
            List<int> tabsadj = new List<int>();
            for (int i = 0; i < tabstops.Count; i++)
                tabsadj.Add((int)(tabstops[i] * (double)fnt[2].SizeInPoints / 8.25));

            lt.SetFormat(row, fnt, cols, tabsadj);
        }

        private void SummaryPopOut_Load(object sender, EventArgs e)
        {
            var top = SQLiteDBClass.GetSettingInt("PopOutFormTop", -1);
            if (top >= 0 )
            {
                var left = SQLiteDBClass.GetSettingInt("PopOutFormLeft", 0);
                var height = SQLiteDBClass.GetSettingInt("PopOutFormHeight", 800);
                var width = SQLiteDBClass.GetSettingInt("PopOutFormWidth", 800);

                // Adjust so window fits on screen; just in case user unplugged a monitor or something

                var screen = SystemInformation.VirtualScreen;
                if (height > screen.Height) height = screen.Height;
                if (top + height > screen.Height + screen.Top) top = screen.Height + screen.Top - height;
                if (width > screen.Width) width = screen.Width;
                if (left + width > screen.Width + screen.Left) left = screen.Width + screen.Left - width;
                if (top < screen.Top) top = screen.Top;
                if (left < screen.Left) left = screen.Left;

                this.Top = top;
                this.Left = left;
                this.Height = height;
                this.Width = width;

                this.CreateParams.X = this.Left;
                this.CreateParams.Y = this.Top;
                this.StartPosition = FormStartPosition.Manual;
            }
        }

        private void SummaryPopOut_FormClosing(object sender, FormClosingEventArgs e)
        {
            SQLiteDBClass.PutSettingInt("PopOutFormWidth", this.Width);
            SQLiteDBClass.PutSettingInt("PopOutFormHeight", this.Height);
            SQLiteDBClass.PutSettingInt("PopOutFormTop", this.Top);
            SQLiteDBClass.PutSettingInt("PopOutFormLeft", this.Left);
        }

        private void UpdateEventsOnControls(Control ctl)
        {
           // Console.WriteLine("Hook " + ctl.ToString() + " " + ctl.Name);
            ctl.MouseEnter -= MouseEnterControl;
            ctl.MouseLeave -= MouseLeaveControl;
            ctl.MouseEnter += MouseEnterControl;
            ctl.MouseLeave += MouseLeaveControl;

            if (ctl is DrawnPanel)
            {
                if (((DrawnPanel)ctl).ImageText != null )   
                {
                    ctl.Click += EDSM_Click;
                }
            }
            else
            { 
                ctl.MouseDown -= MouseDownOnForm;
                ctl.MouseDown += MouseDownOnForm;
                //Console.WriteLine("Hook Mouse down " + ctl.ToString() + " " + ctl.Name);
            }

            foreach (Control ctll in ctl.Controls)
            {
                UpdateEventsOnControls(ctll);
            }
        }


        private void SummaryPopOut_Layout(object sender, LayoutEventArgs e)
        {
            panel_grip.Location = new Point(this.ClientSize.Width - panel_grip.Size.Width, this.ClientSize.Height - panel_grip.Size.Height);
        }

        void FadeOut(object sender, EventArgs e)            // hiding
        {
            panel_grip.Visible = false;
            autofade.Stop();
            this.BackColor = transparentkey;
        }

        private void panel_grip_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                panel_grip.Captured();      // tell drawn panel in a capture
                panel_grip.Capture = false;
                SendMessage(WM_NCL_RESIZE, (IntPtr)HT_RESIZE, IntPtr.Zero);
            }
        }

        private void MouseDownOnForm(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ((Control)sender).Capture = false;
                SendMessage(WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
        }

        private void MouseEnterControl(object sender, EventArgs e)
        {
            autofade.Stop();
            panel_grip.Visible = true;
            this.BackColor = Color.FromArgb(255, 40, 40, 40);
            //Console.WriteLine(Environment.TickCount + " enter" + sender.ToString());
        }

        private void MouseLeaveControl(object sender, EventArgs e)
        {
            if (!ClientRectangle.Contains(this.PointToClient(MousePosition)) && !panel_grip.IsCaptured)
            {
                //Console.WriteLine(Environment.TickCount + " leave " + sender.ToString());
                autofade.Start();
            }
        }

        public void EDSM_Click(object sender, EventArgs e)
        {
            DrawnPanel dp = sender as DrawnPanel;
            Console.WriteLine("EDSM click on " + dp.Name);

            EDSMClass edsm = new EDSMClass();
            string url = edsm.GetUrlToEDSMSystem(dp.Name);

            if (url.Length > 0)         // may pass back empty string if not known, this solves another exception
                System.Diagnostics.Process.Start(url);
            else
                MessageBox.Show("System " + dp.Name + " unknown to EDSM");
        }
    }

    public class ControlTable : IDisposable
    {
        List<ControlRowEntry> entries = new List<ControlRowEntry>();
        Control parent;

        private int maxvertical;
        private int toppos;
        private int vspacing;

        public int Count { get { return entries.Count; } }

        public ControlTable(Control p, int m , int topp , int vspace)
        {
            parent = p;
            toppos = topp;
            maxvertical = m;
            vspacing = vspace;
        }

        public void SetFormat(List<Font> f, List<Color> clist, List<int> tabstops)
        {
            foreach (ControlRowEntry lre in entries)
                lre.SetFormat(f, clist, tabstops);
        }

        public void SetFormat(int row, List<Font> f, List<Color> clist, List<int> tabstops)
        {
            if ( row < entries.Count )
                entries[row].SetFormat(f, clist, tabstops);
        }

        public int Add(List<string> text , int insertpos , int rowid )      // return row that was added..
        {
            if (Full)
            {
                entries[entries.Count-1].Dispose();
                entries.RemoveAt(entries.Count - 1);
            }

            for (int i = insertpos; i < entries.Count; i++)
                entries[i].ShiftVert(vspacing);

            if (insertpos >= entries.Count)
            {
                insertpos = entries.Count;
                entries.Add(new ControlRowEntry(rowid, text, toppos + insertpos*vspacing, vspacing, parent));
                return insertpos;
            }
            else
            {
                entries.Insert(insertpos, new ControlRowEntry(rowid, text, toppos + insertpos*vspacing, vspacing, parent));
                return insertpos;
            }
        }

        public void RemoveAt( int pos )
        {
            for (int i = pos+1; i < entries.Count; i++)
                entries[i].ShiftVert(-vspacing);
            entries[pos].Dispose();
            entries.RemoveAt(pos);
        }

        public void Refresh(List<string> text , int rowid )
        {
            foreach( ControlRowEntry cre in entries )
            {
                if ( cre.RowId == rowid )
                {
                    cre.Refresh(text);
                    break;
                }
            }
        }

        public bool Full { get { return entries.Count >= maxvertical;  } }
        public int Maximum {  get { return maxvertical;  } }

        public void Dispose()
        {
            foreach (ControlRowEntry lre in entries)
                lre.Dispose();

            entries.Clear();
        }
    }

    public class ControlRowEntry : IDisposable
    {
        private List<Control> items;
        private Control parent;
        public int RowId { get; }      // row id of creator..

        public ControlRowEntry(int id , List<string> text, int vpos, int vsize , Control p )
        {
            RowId = id;
            parent = p;

            items = new List<Control>();
            for (int i = 0; i < text.Count; i++)
            {
                if (text[i].Length > 11 && text[i].Substring(0,11).Equals("!!<EDSMBUT:"))
                {
                    ExtendedControls.DrawnPanel edsm = new ExtendedControls.DrawnPanel();
                    edsm.Name = text[i].Substring(11);
                    edsm.Image = DrawnPanel.ImageType.Text;
                    edsm.ImageText = "EDSM";
                    edsm.Size = new Size(100, vsize);
                    edsm.MarginSize = -1;       // 0 is auto calc, -1 is zero
                    edsm.Location = new Point(0, vpos);
                    parent.Controls.Add(edsm);
                    items.Add(edsm);
                }
                else
                {
                    LabelExt lab = new ExtendedControls.LabelExt();
                    lab.Text = text[i];
                    lab.Location = new Point(0, vpos);
                    lab.AutoSize = false;
                    lab.Size = new Size(100, vsize);
                    parent.Controls.Add(lab);
                    items.Add(lab);
                }
            }
        }

        public void Refresh(List<string> text)
        {
            for (int i = 0; i < items.Count; i++)
                items[i].Text = text[i];
        }

        public void SetFormat(List<Font> fnt, List<Color> cl, List<int> tabstops )
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].Font = fnt[i];
                items[i].ForeColor = cl[i];

                if (items[i] is ExtendedControls.DrawnPanel)
                {
                    ExtendedControls.DrawnPanel dp = items[i] as ExtendedControls.DrawnPanel;
                    dp.MouseOverColor = ButtonExt.Multiply(cl[i], 1.3F);
                    dp.MouseSelectedColor = ButtonExt.Multiply(cl[i], 1.5F);
                    dp.BackColor = Color.Black;
                    items[i].Location = new Point(tabstops[i], items[i].Location.Y+3);
                    items[i].Size = new Size(tabstops[i + 1] - tabstops[i] - 4, items[i].Size.Height - 6);
                }
                else
                {
                    items[i].BackColor = Color.Transparent;
                    items[i].Location = new Point(tabstops[i], items[i].Location.Y);
                    items[i].Size = new Size(tabstops[i + 1] - tabstops[i] - 4, items[i].Size.Height - 4);
                }

                items[i].Show();
            }
        }

        public void ShiftVert(int vert)
        {
            for (int i = 0; i < items.Count; i++)
                items[i].Location = new Point(items[i].Location.X, items[i].Location.Y + vert);
        }

        public void Dispose()
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].Hide();
                parent.Controls.Remove(items[i]);
                items[i].Dispose();
            }

            items = null;
        }
    }
}

