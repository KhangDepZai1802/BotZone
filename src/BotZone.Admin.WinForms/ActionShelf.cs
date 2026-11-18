namespace BotZone.Admin.WinForms;

// Measures this compact action area explicitly; an autosized table can retain a
// large empty row when its explanatory label is hidden after a window resize.
internal sealed class ActionShelf : Panel
{
    private readonly Label selection,workflow;
    private readonly FlowLayoutPanel buttons;
    private bool arranging;
    internal ActionShelf(Label selection,FlowLayoutPanel buttons,Label workflow)
    {
        this.selection=selection;this.buttons=buttons;this.workflow=workflow;BackColor=Ui.Surface;AutoSize=true;AutoSizeMode=AutoSizeMode.GrowAndShrink;Margin=new Padding(0,0,0,8);
        selection.AutoSize=workflow.AutoSize=false;buttons.AutoSize=false;buttons.Dock=DockStyle.None;Controls.AddRange([selection,buttons,workflow]);
        selection.TextChanged+=(_,_)=>PerformLayout();workflow.VisibleChanged+=(_,_)=>{PerformLayout();Parent?.PerformLayout();};
    }
    private (int Label,int Buttons,int Workflow,int Gap) Measure(int width)
    {
        var gap=12*DeviceDpi/96;var inner=Math.Max(80,width-gap*2);var h=TextRenderer.MeasureText(selection.Text,selection.Font,new Size(inner,int.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix).Height;
        var x=0;var row=0;var total=0;foreach(Control b in buttons.Controls){if(!b.Visible)continue;var preferred=b.GetPreferredSize(Size.Empty);var bw=preferred.Width+b.Margin.Horizontal;var bh=preferred.Height+b.Margin.Vertical;if(x>0&&x+bw>inner){total+=row;x=0;row=0;}x+=bw;row=Math.Max(row,bh);}total+=row+buttons.Padding.Vertical;
        var wh=workflow.Visible?TextRenderer.MeasureText(workflow.Text,workflow.Font,new Size(inner,int.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.NoPrefix).Height:0;return(h,total,wh,gap);
    }
    public override Size GetPreferredSize(Size proposedSize){if(selection is null)return base.GetPreferredSize(proposedSize);var width=Math.Max(100,(Parent?.ClientSize.Width??Width)-(Parent?.Padding.Horizontal??0));var m=Measure(width);return new Size(width,m.Label+m.Buttons+m.Workflow+m.Gap*2);}
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);if(arranging||selection is null)return;arranging=true;
        try{var m=Measure(ClientSize.Width);var width=Math.Max(1,ClientSize.Width-m.Gap*2);selection.SetBounds(m.Gap,m.Gap,width,m.Label);buttons.SetBounds(m.Gap,m.Gap+m.Label,width,m.Buttons);workflow.SetBounds(m.Gap,m.Gap+m.Label+m.Buttons,width,m.Workflow);var height=m.Label+m.Buttons+m.Workflow+m.Gap*2;if(Height!=height)Height=height;}
        finally{arranging=false;}
    }
}
