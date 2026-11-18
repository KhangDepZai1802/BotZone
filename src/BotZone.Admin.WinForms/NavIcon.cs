namespace BotZone.Admin.WinForms;

internal static class NavIcon
{
    internal static void Draw(Graphics g,string key,Rectangle bounds,Color color)
    {
        var state=g.Save();g.TranslateTransform(bounds.X,bounds.Y);g.ScaleTransform(bounds.Width/24f,bounds.Height/24f);using var p=new Pen(color,1.5f){StartCap=System.Drawing.Drawing2D.LineCap.Round,EndCap=System.Drawing.Drawing2D.LineCap.Round,LineJoin=System.Drawing.Drawing2D.LineJoin.Round};
        if(key=="overview"){foreach(var x in new[]{3,14})foreach(var y in new[]{3,14})g.DrawRectangle(p,x,y,7,7);}
        else if(key is "reports" or "operational_reports"){g.DrawLine(p,3,21,21,21);g.DrawLine(p,6,17,6,11);g.DrawLine(p,12,17,12,4);g.DrawLine(p,18,17,18,8);}
        else if(key is "customers" or "staff"){g.DrawEllipse(p,8,3,8,8);g.DrawArc(p,4,12,16,15,180,180);g.DrawLine(p,4,20,20,20);}
        else if(key is "catalogue" or "inventory" or "categories" or "purchases" or "suppliers"){g.DrawRectangle(p,3,6,18,15);g.DrawLine(p,3,11,21,11);g.DrawLine(p,10,6,10,11);g.DrawLine(p,14,6,14,11);g.DrawLine(p,3,6,6,3);g.DrawLine(p,6,3,18,3);g.DrawLine(p,18,3,21,6);}
        else if(key is "payments" or "refunds" or "promotions"){g.DrawRectangle(p,3,5,18,14);g.DrawLine(p,3,10,21,10);g.DrawLine(p,6,15,10,15);}
        else if(key=="media"){g.DrawRectangle(p,3,3,18,18);g.DrawEllipse(p,7,6,3,3);g.DrawLines(p,[new(4,18),new(10,12),new(14,16),new(18,11),new(21,14)]);}
        else if(key=="landing"){g.DrawRectangle(p,3,3,18,18);g.DrawLine(p,3,8,21,8);g.DrawLine(p,9,8,9,21);}
        else if(key is "settings" or "stock_adjustments"){g.DrawLine(p,3,6,21,6);g.DrawLine(p,3,12,21,12);g.DrawLine(p,3,18,21,18);foreach(var xy in new[]{(8,6),(16,12),(10,18)}){using var b=new SolidBrush(Ui.NavSurface);g.FillEllipse(b,xy.Item1-2,xy.Item2-2,4,4);g.DrawEllipse(p,xy.Item1-2,xy.Item2-2,4,4);}}
        else if(key=="audit"){g.DrawEllipse(p,3,3,18,18);g.DrawLine(p,12,7,12,12);g.DrawLine(p,12,12,16,14);}
        else if(key=="warranty"){g.DrawLines(p,[new(12,2),new(20,6),new(19,15),new(12,22),new(5,15),new(4,6),new(12,2)]);g.DrawLines(p,[new(8,12),new(11,15),new(16,9)]);}
        else if(key=="logout"){g.DrawLines(p,[new(10,3),new(4,3),new(4,21),new(10,21)]);g.DrawLine(p,10,12,22,12);g.DrawLines(p,[new(17,7),new(22,12),new(17,17)]);}
        else if(key is "returns" or "trade_ins"){g.DrawArc(p,4,4,16,16,210,280);g.DrawLines(p,[new(3,3),new(3,10),new(10,10)]);}
        else{g.DrawRectangle(p,5,3,14,18);g.DrawLine(p,9,8,15,8);g.DrawLine(p,9,12,15,12);g.DrawLine(p,9,16,13,16);}
        g.Restore(state);
    }
}
