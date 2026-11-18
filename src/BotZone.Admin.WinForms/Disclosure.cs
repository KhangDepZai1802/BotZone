namespace BotZone.Admin.WinForms;

internal static partial class Ui
{
    internal static Button MenuButton(string label,params (string Label,Action Run)[] items)
    {
        var button=Button(label+" ▾");var menu=new BzContextMenu();
        foreach(var item in items){var command=new ToolStripMenuItem(item.Label){Padding=new Padding(12,7,12,7)};command.Click+=(_,_)=>item.Run();menu.Items.Add(command);}
        button.ContextMenuStrip=menu;button.Click+=(_,_)=>menu.Show(button,new Point(0,button.Height));button.Disposed+=(_,_)=>menu.Dispose();return button;
    }
    internal static void ShowInformation(IWin32Window? owner,string title,string text)
    {
        using var dialog=new Form{Text=title,Size=new Size(600,430),MinimumSize=new Size(420,300),StartPosition=FormStartPosition.CenterParent,MinimizeBox=false,MaximizeBox=false};Prepare(dialog);
        var content=new TextBox{Text=text.ReplaceLineEndings("\r\n"),Multiline=true,ReadOnly=true,BorderStyle=BorderStyle.None,BackColor=Surface,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical,AccessibleName=title};
        var close=Button("Đóng",(_,_)=>dialog.Close());dialog.CancelButton=close;
        var body=new Panel{Dock=DockStyle.Fill,Padding=new Padding(20),BackColor=Surface};body.Controls.Add(content);
        dialog.Controls.Add(Vertical(PageHeader(title,""),body,Bar(close)));
        // The information is selectable/copyable without occupying the task page.
        var layout=(TableLayoutPanel)dialog.Controls[0];layout.RowStyles[1].SizeType=SizeType.Percent;layout.RowStyles[1].Height=100;layout.RowStyles[2].SizeType=SizeType.AutoSize;
        dialog.ShowDialog(owner);
    }
    // Reserve the footer before giving the remaining space to scrollable content.
    // Ui.Vertical intentionally fills its last row, which is unsuitable for dialog actions.
    internal static TableLayoutPanel DialogLayout(Control heading,Control body,params Control[] actions)
    {
        var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(24),BackColor=Paper};
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var footer=Bar(actions);footer.Name="dialogFooter";footer.FlowDirection=FlowDirection.RightToLeft;footer.Margin=new Padding(0,16,0,0);
        heading.Dock=body.Dock=footer.Dock=DockStyle.Fill;body.Margin=Padding.Empty;
        layout.Controls.Add(heading,0,0);layout.Controls.Add(body,0,1);layout.Controls.Add(footer,0,2);return layout;
    }
}

internal sealed class BzContextMenu : ContextMenuStrip
{
    internal BzContextMenu(){Font=new Font("Segoe UI",10.5f);BackColor=Ui.Surface;ForeColor=Ui.Ink;ShowImageMargin=false;Padding=new Padding(6);Renderer=new MenuRenderer();}
    protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width<2||Height<2)return;using var shape=FieldPaint.Shape(new Rectangle(0,0,Width,Height),12);var old=Region;Region=new Region(shape);old?.Dispose();}
    private sealed class MenuRenderer:ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){e.Graphics.Clear(Ui.Surface);}
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){var rect=Rectangle.Inflate(new Rectangle(Point.Empty,e.Item.Size),-2,-1);if(rect.Width<=0||rect.Height<=0)return;using var path=FieldPaint.Shape(rect,8);using var fill=new SolidBrush(e.Item.Selected||e.Item.Pressed?Ui.Soft:Ui.Surface);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;e.Graphics.FillPath(fill,path);}
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){e.TextColor=e.Item.Enabled?Ui.Ink:Ui.Muted;base.OnRenderItemText(e);}
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e){var state=e.Graphics.Save();try{e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;var r=e.ImageRectangle;using var pen=new Pen(Ui.Primary,2);e.Graphics.DrawLines(pen,[new Point(r.Left+3,r.Top+r.Height/2),new Point(r.Left+r.Width/2-1,r.Bottom-4),new Point(r.Right-2,r.Top+3)]);}finally{e.Graphics.Restore(state);}}
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){using var path=FieldPaint.Shape(new Rectangle(0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1),12);using var line=new Pen(Ui.Line);e.Graphics.DrawPath(line,path);}
    }
}
