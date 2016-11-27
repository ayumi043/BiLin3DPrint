using System;
using System.Collections.Generic;
using System.Web;
using Nancy;

namespace WxPayAPI {
    /// <summary>
    /// 扫码支付模式一回调处理类
    /// 接收微信支付后台发送的扫码结果，调用统一下单接口并将下单结果返回给微信支付后台
    /// </summary>
    public class NativeNotify : Notify {
        public NativeNotify(NancyContext context) : base(context) {

        }

        public override Tuple<bool, WxPayData> ProcessNotify() {
            //System.IO.File.WriteAllText($"a{DateTime.Now.ToString("yyyyMMddHHmmssss")}.txt", "hi");
            var result = GetNotifyData();
            var notifyData = result.Item2;
            //System.IO.File.WriteAllText($"b-{DateTime.Now.ToString("yyyyMMddHHmmssss")}.txt", result.Item2.GetValue("out_trade_no").ToString() + "\r\n" + notifyData.ToJson());
            //System.IO.File.WriteAllText($"c{DateTime.Now.ToString("yyyyMMddHHmmssss")}.txt", "result.Item1:" + result.Item1);

            if (result.Item1 == false) {
                return Tuple.Create(false, notifyData);
            }

            //检查openid和product_id是否返回
            //if (!notifyData.IsSet("openid") || !notifyData.IsSet("product_id")) {
            if (!notifyData.IsSet("openid")) {
                WxPayData res = new WxPayData();
                res.SetValue("return_code", "FAIL");
                res.SetValue("return_msg", "回调数据异常");
                Log.Info(this.GetType().ToString(), "The data WeChat post is error : " + res.ToXml());
                return Tuple.Create(false, res);
            }

            /*
            //调统一下单接口，获得下单结果
            string openid = notifyData.GetValue("openid").ToString();
            //string product_id = notifyData.GetValue("product_id").ToString();
            //string product_id = "xxx";
            string orderId = notifyData.GetValue("out_trade_no").ToString();
            WxPayData unifiedOrderResult = new WxPayData();
            try {
                unifiedOrderResult = UnifiedOrder(openid, orderId);
            } catch (Exception ex) {
                //若在调统一下单接口时抛异常，立即返回结果给微信支付后台
                WxPayData res = new WxPayData();
                res.SetValue("return_code", "FAIL");
                res.SetValue("return_msg", "统一下单失败");
                Log.Error(this.GetType().ToString(), "UnifiedOrder failure : " + res.ToXml());
                return Tuple.Create(false, res);
            }

            //若下单失败，则立即返回结果给微信支付后台
            if (!unifiedOrderResult.IsSet("appid") || !unifiedOrderResult.IsSet("mch_id") || !unifiedOrderResult.IsSet("prepay_id")) {
                WxPayData res = new WxPayData();
                res.SetValue("return_code", "FAIL");
                res.SetValue("return_msg", "统一下单失败");
                Log.Error(this.GetType().ToString(), "UnifiedOrder failure : " + res.ToXml());
                return Tuple.Create(false, res);
            }
            

            //统一下单成功,则返回成功结果给微信支付后台
            WxPayData data = new WxPayData();
            data.SetValue("return_code", "SUCCESS");
            data.SetValue("return_msg", "OK");
            data.SetValue("appid", WxPayConfig.APPID);
            data.SetValue("mch_id", WxPayConfig.MCHID);
            data.SetValue("nonce_str", WxPayApi.GenerateNonceStr());
            data.SetValue("prepay_id", unifiedOrderResult.GetValue("prepay_id"));
            data.SetValue("result_code", "SUCCESS");
            data.SetValue("err_code_des", "OK");
            data.SetValue("sign", data.MakeSign());

            Log.Info(this.GetType().ToString(), "UnifiedOrder success , send data to WeChat : " + data.ToXml());
            */

            //System.IO.File.WriteAllText($"aaaaaa-{DateTime.Now.ToString("yyyyMMddHHmmssss")}.txt", result.Item2.GetValue("out_trade_no").ToString() + "\r\n" + notifyData.ToJson());
            return Tuple.Create(true, notifyData);
        }

        private WxPayData UnifiedOrder(string openId, string orderId) {
            //统一下单
            WxPayData req = new WxPayData();
            req.SetValue("body", "test");
            req.SetValue("attach", "test");
            req.SetValue("out_trade_no", orderId);
            req.SetValue("total_fee", 1);
            req.SetValue("time_start", DateTime.Now.ToString("yyyyMMddHHmmss"));
            req.SetValue("time_expire", DateTime.Now.AddMinutes(10).ToString("yyyyMMddHHmmss"));
            req.SetValue("goods_tag", "test");
            req.SetValue("trade_type", "NATIVE");
            req.SetValue("openid", openId);
            req.SetValue("product_id", "xxx");
            WxPayData result = WxPayApi.UnifiedOrder(req);
            return result;
        }
    }
}