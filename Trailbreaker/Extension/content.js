function getPathTo(element) {
    if (element.id !== '')
        return 'id("' + element.id + '")';
    if (element === document.body)
        return element.tagName;

    var ix = 0;
    var siblings = element.parentNode.childNodes;
    for (var i = 0; i < siblings.length; i++) {
        var sibling = siblings[i];
        if (sibling === element)
            return getPathTo(element.parentNode) + '/' + element.tagName + '[' + (ix + 1) + ']';
        if (sibling.nodeType === 1 && sibling.tagName === element.tagName)
            ix++;
    }
}

var entered = "";

document.onkeypress = function notifyType(event) {
    entered += String.fromCharCode(event.keyCode);
};

function getPageObject() {
    var po = "Main";

    var tab = "";
    tab = $("td.tab-c-sel a").text().replace(/[^\w!?]/g, '').toLowerCase();
    tab = tab.charAt(0).toUpperCase() + tab.slice(1);

    po = po.concat(" > " + tab);

    var clientPage = "";
    clientPage = $(".headText").text().replace(/[^\w!?]/g, '');

    po = po.concat(" > " + clientPage);

    var reportTitle = "";
    reportTitle = $("div.reportTitle").text().replace(/[^\w!?]/g, '');

    po = po.concat(" > " + reportTitle);

    var reportName = "";
    reportName = $("#report-name").text().replace(/[^\w!?]/g, '');

    po = po.concat(" > " + reportName);

    var breadCrumb = "";
    breadCrumb = $("span.breadcrumb-item.active").text().replace(/[^\w!?]/g, '');

    po = po.concat(" > " + breadCrumb);
    
    return po;
}

function getPageObject2() {
    var pageObjectIndicators = ["div.page-header h2", "span.breadcrumb-item.active", "#report-name", "div.reportTitle", ".headText", "td.tab-c-sel a"];
    for (var i = 0; i < pageObjectIndicators.length; i++) {
        var po = "";
        if ($(pageObjectIndicators[i]).length > 1) {
            po = $(pageObjectIndicators[i]).eq(0).text().replace(/[^\w!?]/g, '')
        } else {
            po = $(pageObjectIndicators[i]).text().replace(/[^\w!?]/g, '')
        }
        if (po != "") {
//            return pageObjectIndicators[i] + ": " + po;
            return po;
        }
    }
    return "Main";
}

var prev_path = 'undefined';
var count = 0;

function notifyClick(event) {
    if (event === undefined) event = window.event;
    var target = 'target' in event ? event.target : event.srcElement;

    var root = document.compatMode === 'CSS1Compat' ? document.documentElement : document.body;
    var mxy = [event.clientX + root.scrollLeft, event.clientY + root.scrollTop];

    var path = getPathTo(target);

    if (prev_path.valueOf() == path.valueOf()) {
        return;
    }

    prev_path = path.valueOf();
    
    var payload = {
        Name: "item" + count,
        Page: getPageObject2(),
        Node: target.nodeName.valueOf(),
        Type: "" + target.getAttribute("type"),
        Path: path.valueOf(),
        Text: entered.valueOf()
    };
    
    $.ajax({
        type: "POST",
        url: "http://localhost:8055/",
        data: JSON.stringify(payload),
        contentType: "application/json; charset=utf-8",
        dataType: "json"
    });

    count++;
    entered = "";
}

function loadElems() {
    a_elems = document.getElementsByTagName("a");
    for (i = 0, max = a_elems.length; i < max; i++) {
        a_elems[i].onclick = notifyClick;
    }
    img_elems = document.getElementsByTagName("img");
    for (i = 0, max = img_elems.length; i < max; i++) {
        img_elems[i].onclick = notifyClick;
    }
    div_elems = document.getElementsByTagName("div");
    for (i = 0, max = div_elems.length; i < max; i++) {
        div_elems[i].onclick = notifyClick;
    }
    document.onclick = notifyClick;
}

loadElems();