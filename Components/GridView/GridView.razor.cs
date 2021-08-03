using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngryMonkey.Cloud.Components
{
    public partial class GridView
    {
        private int CurrentPage = 1;
        private int StartPage;
        private int EndPage;
        public string Boxchecked;

        private bool IsNavigateForward
        {
            get
            {
                if (CurrentPage == PageSize)
                    return false;
                return true;

            }
        }
        private bool IsNavigateBackward
        {
            get
            {
                if (CurrentPage == 1)
                    return false;
                return true;

            }
        }
        private int StartCount
        {
            get
            {
                if (CurrentPage == 1)
                    return 1;

                
                return ( PageSize * CurrentPage) - PageSize + 1;
            }
        }

        private int EndCount
        {
            get
            {
                return StartCount + PageSize -1;
            }
        }

        private double TotalPages
        {
            get
            {
                return (int)Math.Ceiling(RowsCount / (decimal)PageSize);
            }
        }
       
        [Parameter] public GridViewColumns Columns { get; set; }
        [Parameter] public GridViewRows Rows { get; set; }
        [Parameter] public int PageSize { get; set; }
        [Parameter] public int RowsCount { get; set; }
        [Parameter] public EventCallback<GridViewRequestDataEventArgs> OnRequestData { get; set; }

        protected void SelectRow(GridViewRow row)
        {
            if (row.Selected)
            {
                row.Selected = false;
                Boxchecked = "";
                return;
            }

            //foreach (GridViewRow r in Item.Rows)
            //    r.Selected = false;

            row.Selected = true;
            Boxchecked = "checked";

        }
        protected void click()
        {
            foreach (GridViewRow row in Rows.Rows)
            {
                if (row.Selected)
                {
                    Console.WriteLine(row.Cells[0].Value);
                    Console.WriteLine(row.Selected);
                    Console.WriteLine(row.CssClasses);
                }
            }
        }

        protected override async Task OnInitializedAsync()
        {
           
            GridViewRequestDataEventArgs changeArgs = new()
            {
                PageSize = PageSize,
                PageNumber = CurrentPage
            };

            await OnRequestData.InvokeAsync(changeArgs);
            //  SetPagerSize("forward");
        }

        public async Task updateList(int currentPage)
        {
            GridViewRequestDataEventArgs changeArgs = new()
            {
                PageSize = PageSize,
                PageNumber = currentPage

            };

            await OnRequestData.InvokeAsync(changeArgs);
            CurrentPage = currentPage;

        }

        //public async Task SetPagerSize(string direction)
        //{
        //    if (direction == "forward" && EndPage < TotalPages)
        //    {
        //        StartPage = EndPage + 1;
        //        if (EndPage + PageSize < TotalPages)
        //            EndPage = StartPage + PageSize - 1;

        //        else
        //            EndPage = TotalPages;

        //        await updateList(EndPage);
        //    }

        //    else if (direction == "back" && StartPage > 1)
        //    {
        //        EndPage = StartPage - 1;
        //        StartPage = StartPage - PageSize;

        //        await updateList(StartPage);
        //    }
        //}

        public async Task NavigateToPage(string direction)
        {
            if (direction == "next")
            {
                if (CurrentPage < TotalPages)
                {
                    //if (CurrentPage == endPage)
                    //{
                    //    //SetPagerSize("forward");
                    //}
                    CurrentPage += 1;
                }
            }
            else if (direction == "previous")
            {
                if (CurrentPage > 1)
                {
                    //if (CurrentPage == startPage)
                    //{
                    //    // SetPagerSize("back");
                    //}
                    CurrentPage -= 1;
                }
            }

            await updateList(CurrentPage);
        }
    }
}
