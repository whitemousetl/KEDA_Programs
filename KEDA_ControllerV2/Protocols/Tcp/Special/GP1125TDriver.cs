//using HslDesign;
//using KEDA_CommonV2.Entity;
//using KEDA_CommonV2.Model;
//using KEDA_ControllerV2.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Drawing.Printing;
//using System.Xml.Linq;
//using System.Drawing;
//using KEDA_CommonV2.Enums;
//using KEDA_CommonV2.Interfaces;

//namespace KEDA_ControllerV2.Protocols.Special;
//[ProtocolType(ProtocolType.GP1125T)]
//public class GP1125TDriver : IProtocolDriver//托码，标签打印机
//{
//    public Task<ProtocolResult?> ReadAsync(Protocol protocol, string devId, PointEntity point, CancellationToken token)
//    {
//        throw new NotImplementedException();
//    }

//    public async Task<bool> WriteAsync(WriteTaskEntity writeTask, CancellationToken token)
//    {
//        var xmlPath = @"D:\ThreeCode\PrintTemplate.xml";
//        HslDesignCore designCore = new(XElement.Load(xmlPath));

//        var values = new Dictionary<string, object>();

//        if (writeTask.WriteDevice == null) return false;

//        foreach (var point in writeTask.WriteDevice.WritePoints)
//        {
//            values[point.Label] = point.Value;
//        }

//        designCore.SetDictionaryValues(values);

//        PrintDocument print = new PrintDocument();
//        print.PrintPage += (object sender, PrintPageEventArgs e) =>
//        {
//            PaintResource paintResource = new PaintResource();
//            paintResource.Width = designCore.DesignWidth;
//            paintResource.Height = designCore.DesignHeight;
//            paintResource.DefaultFont = new Font("微软雅黑", 9f);
//            paintResource.G = e.Graphics;

//            // 如果需要平滑绘制，可以写下面两行代码
//            // paintResource.G.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
//            // paintResource.G.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

//            designCore.DrawDesign(paintResource);
//        };
//        print.PrinterSettings.PrinterName = writeTask.WriteDevice.DeviceId;

//        print.Print();

//        await Task.CompletedTask;

//        return true;
//    }

//    public void Dispose()
//    {
//        // 当前无需要释放的资源
//    }

//    public string GetProtocolName()
//    {
//        return "GP1125T";
//    }
//}