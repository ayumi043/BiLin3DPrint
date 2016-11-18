/* Write here your custom javascript codes */

var getcar = function () {
    $.get("/car/get", function (data) {
        if (data.length > 0) {
            var _html = "";
            var _totalnum = 0
            $.each(data, function (index, item) {
                _html += "<li class='carli'>" +
                            "3D打印产品" + "<div class='pull-right'>￥" + item.price + "元 x" + item.num + "<span style='padding-left:10px;'><a class='delcar' style='color:red;' delurl='/car/material/del/" + item.carDetailId + "-" + item.carId + "'>删除</a></span></div>" + "<br>" +
                            "- 文件:" + item.fileName + "<span>" + "<br>" +
                            "- 材料:" + item.matName + "<br>" +
                            "- 颜色:" + item.matColor + "<br>" +
                            "- 精度:" + item.accuracy + "<br>" +
                            "- 交货周期:" + item.delivery + "<br>" +
                         "</li>";
                _totalnum += parseInt(item.num);
            });
            $(".totalnum").html(_totalnum);
            $(".totalprice").html(data[0].amount);
            $("#shopcar-list").html(_html).show();
            $("#gocheck").show();
        }
    });
};
//setTimeout(getcar(),200)
getcar();

$(function () {

    //if (getcar) {
    //    getcar();
    //}

    $("body").on("click", ".delcar", function () {
        var $this = $(this);
        $.post($this.attr("delurl"), function (data) {
            $this.parents(".carli").slideUp(function () {
                $(this).remove();
            });
            $(".totalnum").html(data.sumNum);
            $(".totalprice").html(data.sumAmount);
        });
    });
});

$(function () {
    $("body").on("click", ".prev", function () {
        var $num = parseInt($(this).next(".num").val());
        var $price = parseFloat($(this).nextAll(".price").val());
        var $price1 = parseFloat($(this).nextAll(".price1").val());

        if ($num == 1) {
            $(this).parents("td").next(".AmountDetail").find("span").html($price);
            return;
        }
        $(this).next(".num").val($num - 1);

        var sumPrice = ($num - 1) * parseFloat($price);
        var _price;
        //if (sumPrice < $price1) {
        //    _price = $price1
        //} else {
        //    _price = sumPrice
        //}
        _price = sumPrice
        $(this).parents("td")
            .next(".AmountDetail")
            .find("span")
            .html(_price);

        var _p = 0;
        $(".amount-detail").each(function () {
            _p = _p + parseFloat($(this).html());
        });
        $(".totalprice").html(_p);
        //alert(_price);
    });
    $("body").on("click", ".next", function () {
        var $num = parseInt($(this).prev(".num").val());
        var $price = parseFloat($(this).nextAll(".price").val());
        var $price1 = parseFloat($(this).nextAll(".price1").val());

        $(this).prev(".num").val($num + 1);

        var sumPrice = ($num + 1) * parseFloat($price);
        var _price;
        //if (sumPrice < $price1) {
        //    _price = $price1
        //} else {
        //    _price = sumPrice
        //}
        _price = sumPrice
        $(this).parents("td")
            .next(".AmountDetail")
            .find("span")
            .html(_price);

        var _p = 0;
        $(".amount-detail").each(function () {
            _p = _p + parseFloat($(this).html());
        });
        $(".totalprice").html(_p);
    });
});
