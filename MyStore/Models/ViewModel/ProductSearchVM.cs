using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PagedList.Mvc;

namespace MyStore.Models.ViewModel
{
    public class ProductSearchVM
    {
        //tiêu chí để search theo tên, mô tả sản phẩm hoặc loại sản phẩm
        public string SearchTerm { get; set; }
        public int? CategoryID { get; set; }
        public string CategoryName { get; set; }

        //các tiêu chí để search theo giá
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }        

        //Thứ tự sắp xếp
        public string SortOrder { get; set; }

        //các thuộc tính hỗ trợ phân trang
        public int PageNumber { get; set; }
        public int PageSize { get; set; } = 10;

        //danh sách sản phẩm đã phân trang
        public PagedList.IPagedList<Product> Products { get; set; }

        //danh sách sản phẩm thỏa điều kiện tìm kiếm
        //public List<Product> Products { get; set; }
    }
}