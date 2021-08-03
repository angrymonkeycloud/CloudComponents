//const table : HTMLElement= document.getElementById('table');
//resizableGrid(table);

export function resizableGrid(table): void {
    debugger;
    var row = table.getElementsByTagName('tr')[0],
        cols = row ? row.children : undefined;
    if (!cols) return;
    for (var i = 0; i < cols.length; i++) {
        var div = createDiv(table.offsetHeight);
        cols[i].appendChild(div);
        cols[i].style.position = 'relative';
        setListeners(div);
    }
}

    function createDiv(height):HTMLDivElement {
        var div = document.createElement('div');
        div.style.top = '0';
        div.style.right = '0';
        div.style.width = '5px';
        div.style.position = 'absolute';
        div.style.cursor = 'col-resize';
        /* remove backGroundColor later */
        div.style.backgroundColor = 'red';
        div.style.userSelect = 'none';
        /* table height */
        div.style.height = height + 'px';
        return div;
    }
   function setListeners(div):void {
        var pageX, curCol, nxtCol, curColWidth, nxtColWidth;
        div.addEventListener('mousedown', function (e) {
            curCol = e.target.parentElement;
            nxtCol = curCol.nextElementSibling;
            pageX = e.pageX;
            curColWidth = curCol.offsetWidth
            if (nxtCol)
                nxtColWidth = nxtCol.offsetWidth
        });

        document.addEventListener('mousemove', function (e) {
            if (curCol) {
                var diffX = e.pageX - pageX;

                if (nxtCol)
                    nxtCol.style.width = (nxtColWidth - (diffX)) + 'px';

                curCol.style.width = (curColWidth + diffX) + 'px';
            }
        });

        document.addEventListener('mouseup', function (e) {
            curCol = undefined;
            nxtCol = undefined;
            pageX = undefined;
            nxtColWidth = undefined;
            curColWidth = undefined;
        });
    }

