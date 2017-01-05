using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nancy;

namespace BiLin3D.Lib {

    //pagelist = new PagedList<Shop>() {      
    //    models = shops_v1.ToList(),
    //    total_count = _total_count,
    //    page_size = _pagesize
    //};
    public class PagedList<T> {
        //public IList models {get;set;}
        public List<T> models { get; set; }
        public int total_count { get; set; }
        public int page_size { get; set; }
        public string Pagination(Request ctx) {
            int total_page = (int)Math.Ceiling((double)this.total_count / this.page_size);
            int threshold = 7;        // 设置阀值
            int half_threshold = 3;   // 一半阀值

            if (total_page <= 1) {
                return "";            // 只有1页就不显示
            }

            int current = 1;
            if (ctx.Query["page"] != null) {
                int.TryParse(ctx.Query["page"], out current);
            }

            // 方法1：直接修改/附加 page参数    方法2: 使用ctx.Query获取查询参数，并重新拼凑所有参数和page参数
            string url = ctx.Path + ctx.Url.Query;
            if (Regex.IsMatch(url, @"page=\d+")) {
                url = Regex.Replace(url, @"page=\d+", "page={0}");
            } else {
                if (url.Contains('?')) {
                    url = url + "&page={0}";
                } else {
                    url = url + "?page={0}";
                }
            }

            //if (current > total_page) {
            //    current = total_page;
            //}
            //if (current < 1) {
            //    current = 1;
            //}

            int begin = 1, end = 1;
            if (current <= half_threshold && total_page <= threshold) {
                begin = 1;
                end = total_page;
            } else if (current <= half_threshold && total_page > threshold) {
                begin = 1;
                end = threshold;
            } else if (total_page - current < half_threshold) {
                begin = total_page - threshold;
                if (begin < 1) {
                    begin = 1;
                }
                end = total_page;
            } else {
                begin = current - half_threshold;
                end = current + half_threshold;
            }

            string str = "";
            if (current == 1) {
                str = @"<nav>
                        <ul class='pagination' style='margin-top:0px;float:left;'>
                        <li>
                            <a aria-label='Previous'>
                            <span aria-hidden='true'>&laquo;</span>
                            </a>
                        </li>";
            } else {
                str = string.Format(@"
                    <nav>
                        <ul class='pagination' style='margin-top:0px;float:left;'>
                        <li>
                            <a href='{0}' aria-label='Previous'>
                            <span aria-hidden='true'>&laquo;</span>
                            </a>
                        </li>", url);
                str = string.Format(str, current - 1);
            }


            for (int i = begin; i < end + 1; i++) {
                if (current == i) {
                    str += string.Format(@"<li class='active'><a>{1}</a></li>", url, i);
                    continue;
                }
                str += string.Format(@"<li><a href='{0}'>{1}</a></li>", url, i);
                str = string.Format(str, i);
            }


            if (current == total_page) {
                str += $@"   <li>
                            <a aria-label='Next'>
                            <span aria-hidden='true'>&raquo;</span>
                            </a>
                        </li>
                        </ul>
                        <span style='float:left;padding:6px 0px 0px 18px;'>每页 <b>{page_size}</b> 条&nbsp;-&nbsp;共 <b>{total_count}</b> 条</span>
                    </nav>";
            } else {
                str += string.Format(@"
                        <li>
                            <a href='{0}' aria-label='Next'>
                            <span aria-hidden='true'>&raquo;</span>
                            </a>
                        </li>                       
                        </ul>
                        <span style='float:left;padding:6px 0px 0px 18px;'>每页 <b>{2}</b> 条&nbsp;-&nbsp;共 <b>{1}</b> 条</span>
                    </nav>", url, total_count, page_size);
                str = string.Format(str, current + 1);
            }

            return str;
        }
    }
}
