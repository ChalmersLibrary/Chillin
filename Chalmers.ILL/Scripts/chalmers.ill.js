// Local Javascript

function animateLoadingBallCb() {
    this.animate(this.idleAttr, 500, mina.linear);
    this.next && this.next.animate(this.expandedAttr, 500, mina.linear, animateLoadingBallCb);
}

// DOM READY
$(function () {

    // Every time document loads, release possible current Member locks
    $(document).ready(function () {

        // Create the SVG image for the loading animation and start animating it in the backgorund
        var s = Snap("#chilli");
        var numberOfBalls = 8;
        var angle;

        var ballIdle = { r:4, fill: "#1e1e1e", opacity: 0.2 };
        var ballExpanded = { r: 12, fill: "#19B9E6", opacity: 1 };
        var prev = s.circle(50 + Math.cos(0) * 30, 50 + Math.sin(0) * 30, ballIdle.r);
        prev.attr(ballIdle);
        prev.idleAttr = ballIdle;
        prev.expandedAttr = ballExpanded;

        var curr;
        var first = prev;
        for (i = 1; i < numberOfBalls; i++) {
            angle = Math.PI * 2 * (i / numberOfBalls);
            curr = s.circle(50 + Math.cos(angle) * 30, 50 + Math.sin(angle) * 30, ballIdle.r);
            curr.attr(ballIdle);
            curr.idleAttr = ballIdle;
            curr.expandedAttr = ballExpanded;
            prev.next = curr;
            prev = curr;
        }
        curr.next = first;

        first.animate(ballExpanded, 500, mina.linear, animateLoadingBallCb);


        // Set up the filter buttons
        var btnGroup = $("#filter-buttons");
        $("button", btnGroup).each(function () {
            var button = $(this);
            button.click(function () {
                $("button", btnGroup).each(function () {
                    $(this).removeClass("active");
                });
                button.addClass("active");

                // Close potential open order items
                if ($(".order-list > div[class~='open']").length > 0) {
                    $("#filter-buttons").data("pending-filter-change", "1");
                    closeOrderItem($(".order-list > div[class~='open']"));
                } else {
                    applyListFilter(button.val(), true);
                }
            });
        });
        updateFilterButtonCounters();

        // Set up the library filter buttons
        var libBtnGroup = $("#library-filter-buttons");
        $("button", libBtnGroup).each(function () {
            var button = $(this);
            button.click(function () {
                $("button", libBtnGroup).each(function () {
                    $(this).removeClass("active");
                });
                button.addClass("active");

                // Close potential open order items
                if ($(".order-list > div[class~='open']").length > 0) {
                    $("#library-filter-buttons").data("pending-filter-change", "1");
                    closeOrderItem($(".order-list > div[class~='open']"));
                } else {
                    applyLibraryListFilter(button.val(), true);
                }
            });
        });
        updateLibraryFilterButtonCounters();


        // Set up the sorting links
        function sortClickEventFunc(elem, sortFunc) {
            // Close potential open order items
            if ($(".order-list > div[class~='open']").length > 0) {
                closeOrderItem($(".order-list > div[class~='open']"));
            }
            $("#sort-icon").remove();
            $(elem).html($(elem).html() + "<span id=\"sort-icon\" class=\"glyphicon glyphicon-chevron-down\"></span>");
            $(".order-list > .illedit").sort(sortFunc).each(function () {
                $(this).parent().append(this);
            });
        }

        $("#sort-on-follow-up-link").click(function () {
            sortClickEventFunc(this, function (a, b) {
                return parseInt($(a).find("div[data-column='createDate']").data("fud")) - parseInt($(b).find("div[data-column='createDate']").data("fud"));
            })
        });
        $("#sort-on-type-link").click(function () {
            sortClickEventFunc(this, function (a, b) {
                var aType = $(a).find("div[data-column='type']").text();
                var bType = $(b).find("div[data-column='type']").text();
                var result = aType < bType ? -1 : (aType > bType ? 1 : 0);
                if (result == 0) {
                    result = parseInt($(a).find("div[data-column='createDate']").data("fud")) - parseInt($(b).find("div[data-column='createDate']").data("fud"))
                }
                return result;
            })
        });
        $("#sort-on-status-link").click(function () {
            sortClickEventFunc(this, function (a, b) {
                var aStatus = parseInt($(a).find(".order-item-status").attr("class").match(/chillin-status-([0-9]{2})/)[1]);
                var bStatus = parseInt($(b).find(".order-item-status").attr("class").match(/chillin-status-([0-9]{2})/)[1]);
                
                // Fix the fact that the status indexes does not always decide the sorting order
                aStatus = aStatus * 100;
                bStatus = bStatus * 100;
                if (aStatus == 900)
                {
                    aStatus = 450;
                }
                if (bStatus == 900)
                {
                    bStatus = 450;
                }

                var result = aStatus - bStatus;
                if (result == 0) {
                    result = parseInt($(a).find("div[data-column='createDate']").data("fud")) - parseInt($(b).find("div[data-column='createDate']").data("fud"))
                }
                return result;
            })
        });


        // First unlock all visible locked items
        $(".illedit[data-locked-by-memberid]").each(function () {
            var node = $(this);
            $.getJSON("/umbraco/surface/OrderItemSurface/UnlockOrderItem?nodeId=" + node.attr("id"), function (json) {
                if (json.Success) {
                    node.removeAttr("data-locked-by-memberid");
                }
                else {
                    alert(json.Message);
            }
            });
        });

            // Discover all remaining locks for Current Member
        var lockedNodes = new Array();
        $.getJSON("/umbraco/surface/OrderItemSurface/GetLocksForCurrentMember", function (json) {
            if (json.Success) {
                $.each(json.List, function (index, lock) {
                    lockedNodes.push(lock);
                });
            }
            else {
                alert(json.Message);
        }
        }).done(function () {
            if (lockedNodes && lockedNodes.length) {
                $.each(lockedNodes, function (index, lock) {
                    $.getJSON("/umbraco/surface/OrderItemSurface/UnlockOrderItem?nodeId=" + lock.NodeId, function (json) {
                        if (!json.Success) {
                            alert(json.Message);
                    }
                    });
                });
        }
        });

    });

    $(".illedit").on("click", ".reflink", function (event) {
        event.stopPropagation();
            });

    // Logged in Member clicks an OrderItem Summary row
    $(".illedit").click(function () {
        $(".silly-filler").css("height", "");
        // Only trigger click if no text is selected.
        var sel = getSelection().toString();
        if (!sel) {
        // If clicked OrderItem Details is OPEN, just close it
        if ($(this).hasClass('open')) {
            closeOrderItem(this);
        }

        // If clicked OrderItem Details is CLOSED, open it but close all others on screen
        else {
            // Close all open edit-modes and remove styles and classes
            $(".illedit").css("background-color", "");
            $(".illeditbutton").removeClass("glyphicon-chevron-up").addClass("glyphicon-chevron-down");
            $(".editmode").remove();
            $(".illedit").removeClass('open');

            // Unlock the OrderItems where we have lock
            $(".illedit[data-locked-by-memberid]").each(function () {
                var node = $(this);
                $.getJSON("/umbraco/surface/OrderItemSurface/UnlockOrderItem?nodeId=" + node.attr("id"), function (json) {
                    if (json.Success) {
                        node.removeAttr("data-locked-by-memberid");
                    }
                    else {
                        alert(json.Message);
                    }
                });
            });

            // Get the current id and set styles and classes
            var id = $(this).attr("id");
            $(this).css("background-color", "#dddddd");
            $(this).find(".illeditbutton").removeClass("glyphicon-chevron-down").addClass("glyphicon-chevron-up");
            $(this).addClass('open');

            // Open up the edit-mode for the current id
            $("#" + id).after("<div id='edit-" + id + "' class='ajax-partial-view-content row editmode'>Du &ouml;ppnar upp redigeringsl&auml;ge f&ouml;r nodid " + id + "</div>");
            loadOrderItemDetails(id, function () {
                // Scroll open item to top of browser window
                var openOrderItemSummary = $(".illedit.open");
                var openOrderItemDetails = $(".editmode");
                var orderItemHeight = openOrderItemSummary.height() + openOrderItemDetails.height();
                var freeBottomSpace = Math.max(0, $(".silly-filler").offset().top + parseInt($("body").css("margin-bottom"), 10) - (openOrderItemDetails.offset().top + openOrderItemDetails.height()));
                var missingHeightAtBottom = Math.max(0, $(window).height() - 50 - orderItemHeight - freeBottomSpace);
                $(".silly-filler").css("height", missingHeightAtBottom.toString() + "px");
                $('html, body').animate({
                    scrollTop: openOrderItemSummary.offset().top - 50
                }, 0);
            });
        }
        }
    });

    // Disable links that need to be disabled, ie in dropdown menus
    $(".disabled-link").click(function (event) {
        event.preventDefault();
    });
});

// Create object containing the DOM object from document and also a timer.
var $docTimer = $(document), timer;

function fullSiteBusyAnimationStart()
{
    timer && clearTimeout(timer);
    timer = setTimeout(function () {
        $("#lockscreen").animate({
            opacity: 1
        }, {
            duration: 400
        });
    },
    1000);
}

function fullSiteBusyAnimationStop()
{
    $("#lockscreen").stop(true);
    clearTimeout(timer);
    $("#lockscreen").hide();
    $("#busylock").hide();
    $("#lockscreen").css("opacity", 0);
}

function closeOrderItem(elem)
{
    $(".illedit").css("background-color", "");
    $(".illeditbutton").removeClass("glyphicon-chevron-up").addClass("glyphicon-chevron-down");
    $(".editmode").remove();
    $(".illedit").removeClass('open');

    // Unlock the OrderItems where we have lock
    if ($(elem).attr("data-locked-by-memberid")) {
        var node = $(elem);
        $.getJSON("/umbraco/surface/OrderItemSurface/UnlockOrderItem?nodeId=" + node.attr("id"), function (json) {
            if (json.Success) {
                node.removeAttr("data-locked-by-memberid");
            }
            else {
                alert(json.Message);
            }
        });
    }
}

var lockLevel = 0;
function lockScreen() {
    if (lockLevel == 0) {
        $("#lockscreen").show();
        $("#busylock").show();
        fullSiteBusyAnimationStart();
    }
    lockLevel++;
}

function unlockScreen() {
    lockLevel--;
    if (lockLevel == 0) {
        fullSiteBusyAnimationStop();
        $("#lockscreen").hide();
        $("#busylock").hide();
    }
}

function replaceURLWithHTMLLinks(text) {
    var exp = /(\b(https?|ftp|file):\/\/[-A-Z0-9+&@#\/%?=~_|!:,.;()]*[-A-Z0-9+&@#()\/%=~_|()])/ig;
    return text.replace(exp, "<a target='blank' class='reflink' href='$1'>$1</a>");
}

function addSaveDocumentButton(id, text) {
    var re = /href=['"]([^'"]*)['"]>[^<]*<\/a>/g;
    while (m = re.exec(text)) {
        var pos = m.index + m[0].length;
        text = text.substr(0, pos) + " <button type=\"button\" class=\"btn btn-default\" onclick=\"event.stopPropagation(); saveDocument(" + id + ",'" +  m[1] + "');\"><span class=\"glyphicon glyphicon-cloud-upload\"></span></button> " + text.substr(pos);
    }
    return text;
}

function saveDocument(nodeId, url) {
    lockScreen();
    $.post("/umbraco/surface/ImportDocumentSurface/ImportFromUrl", { orderItemNodeId: nodeId, url: url }).done(function (json) {
        if (json.Success) {
            loadOrderItemDetails(nodeId);
        }
        else {
            alert(json.Message);
        }
        unlockScreen();
    }).error(unlockScreen);
}

function uploadDocument(nodeId, name, data) {
    lockScreen();
    $.post("/umbraco/surface/ImportDocumentSurface/ImportFromData", { orderItemNodeId: nodeId, filename: name, data: data }).done(function (json) {
        if (json.Success) {
            var msgArr = json.Message.split(";", 2);
            if ($(".drm-warning").length > 0) {
                $("#attachments-group").append("<div class=\"btn-group\">\n" +
                        "<button type=\"button\" class=\"btn btn-default btn-attach-attachment btn-info active\" onclick=\"toggleDocumentSendStatus(this);\" data-media-id=\"" + msgArr[0] + "\">" + name + " <span class=\"glyphicon glyphicon-paperclip\"></span></button>\n" +
                        "<button type=\"button\" class=\"btn btn-default btn-view-attachment btn-danger\" onclick=\"openDocument(this);\" data-link=\"" + msgArr[1] + "\"><span class=\"glyphicon glyphicon-search\"></span></button>\n" +
                    "</div>\n");
                $("#attachments-group2").append("<button type=\"button\" class=\"btn btn-default btn-view-attachment btn-danger\" onclick=\"openDocument(this);\" data-link=\"" + msgArr[1] + "\" data-media-id=\"" + msgArr[0] + "\">" + name + " <span class=\"glyphicon glyphicon-search\"></span></button>");
                $("#attachment-counter1").text(parseInt($("#attachment-counter1").text()) + 1);
            }
            else
            {
                $("#attachments-group").append("<div class=\"btn-group\">\n" +
                        "<button type=\"button\" class=\"btn btn-default btn-attach-attachment btn-info active\" onclick=\"toggleDocumentSendStatus(this);\" data-media-id=\"" + msgArr[0] + "\">" + name + " <span class=\"glyphicon glyphicon-paperclip\"></span></button>\n" +
                        "<button type=\"button\" class=\"btn btn-default btn-view-attachment\" onclick=\"openDocument(this);\" data-link=\"" + msgArr[1] + "\"><span class=\"glyphicon glyphicon-search\"></span></button>\n" +
                    "</div>\n");
                $("#attachments-group2").append("<button type=\"button\" class=\"btn btn-default btn-view-attachment\" onclick=\"openDocument(this);\" data-link=\"" + msgArr[1] + "\" data-media-id=\"" + msgArr[0] + "\">" + name + " <span class=\"glyphicon glyphicon-search\"></span></button>");
                $("#attachment-counter1").text(parseInt($("#attachment-counter1").text()) + 1);
            }
        }
        else {
            alert(json.Message);
        }
        unlockScreen();
    }).error(unlockScreen);
}

function takeOverLockedOrderItem(id)
{
    $.getJSON("/umbraco/surface/OrderItemSurface/TakeOverLockedOrderItem?nodeId=" + id, function (json) {
        alert(json.Message);
        if (json.Success) {
            loadOrderItemDetails(id);
        }
        else {
            alert(json.Message);
        }
    });
}

function loadOrderItemDetails(id, cb)
{
    $('#edit-' + id + '.ajax-partial-view-content').load("/umbraco/surface/OrderItemSurface/RenderOrderItem?nodeId=" + id,
        function (responseText, textStatus, req) {
            // req.status:403, req.statusText:Forbidden
            if (req.status != 200) {
                alert("Status: " + req.status + ", reason: " + req.statusText + "\nYou don't have access to view this data or you are not logged in.");
            } else {
                if (cb) cb();
            }
        }
    );
}

function reapplyListFilterAndUpdateCounters()
{
    // Check if we have a manually triggered filter change pending, if so we should animate
    var animate = $("#filter-buttons").data("pending-filter-change") == "1";
    $("#filter-buttons").data("pending-filter-change", "0");
    applyListFilter($("#filter-buttons").find("[class~='active']").attr("value"), animate);
    updateFilterButtonCounters();
}

function reapplyLibraryListFilterAndUpdateCounters() {
    // Check if we have a manually triggered filter change pending, if so we should animate
    var animate = $("#library-filter-buttons").data("pending-filter-change") == "1";
    $("#library-filter-buttons").data("pending-filter-change", "0");
    applyLibraryListFilter($("#library-filter-buttons").find("[class~='active']").attr("value"), animate);
    updateLibraryFilterButtonCounters();
}

function reapplyFiltersAndUpdateCounters() {
    var statusfilterupdate = $("#filter-buttons").data("pending-filter-change") == "1";
    var libraryfilterupdate = $("#library-filter-buttons").data("pending-filter-change") == "1";
    if (statusfilterupdate) {
        $("#filter-buttons").data("pending-filter-change", "0");
        applyListFilter($("#filter-buttons").find("[class~='active']").attr("value"), true);
    } else if (libraryfilterupdate) {
        $("#library-filter-buttons").data("pending-filter-change", "0");
        applyLibraryListFilter($("#library-filter-buttons").find("[class~='active']").attr("value"), true);
    }
    updateFilterButtonCounters();
    updateLibraryFilterButtonCounters();
}

function resetListFilterAndUpdateCounters()
{
    $("#filter-buttons").find("[class~='active']").removeClass("active");
    $("#status00-button").addClass("active");
    applyListFilter("", false);
    updateFilterButtonCounters();
}

function resetFilterButtons()
{
    $("#library-filter-buttons").find(".active").removeClass("active");
    $("#library00-button").addClass("active");
    $("#filter-buttons").find(".active").removeClass("active");
    $("#status00-button").addClass("active");
}

function applyListFilter(filter, animate)
{
    // If we have more than 50 items in the list we don't want to animate.
    if ($(".order-list").find(".order-item-status").length > 50)
    {
        animate = false;
    }

    var libfilter = $("#library-filter-buttons").find(".active").val();

    if (filter === "") {
        animate ? $(".order-list").find(".order-item-status").parent().parent().slideDown() : $(".order-list").find(".order-item-status").parent().parent().show();
        if (libfilter != "") {
            //animate ? $(".order-list").find(".deliveryLibrary:not(" + libfilter + ")").parent().slideUp() :
            $(".order-list").find(".deliveryLibrary:not(" + libfilter + ")").parent().hide();
            // animate ? $(".order-list").find(".deliveryLibrary" + libfilter).parent().slideDown() : $(".order-list").find(".deliveryLibrary" + libfilter).parent().show();
        }
    } else {
        animate ? $(".order-list").find(".order-item-status:not(" + filter + ")").parent().parent().slideUp() : $(".order-list").find(".order-item-status:not(" + filter + ")").parent().parent().hide();
        animate ? $(".order-list").find(".order-item-status" + filter).parent().parent().slideDown() : $(".order-list").find(".order-item-status" + filter).parent().parent().show();
        if (libfilter != "") {
            //animate ? $(".order-list").find(".deliveryLibrary:not(" + libfilter + ")").parent().slideUp() :
            $(".order-list").find(".deliveryLibrary:not(" + libfilter + ")").parent().hide();
           // animate ? $(".order-list").find(".deliveryLibrary" + libfilter).parent().slideDown() : $(".order-list").find(".deliveryLibrary" + libfilter).parent().show();
        }
    }
}

function applyLibraryListFilter(filter, animate)
{
    // If we have more than 50 items in the list we don't want to animate.
    if ($(".order-list").find(".order-item-status").length > 50) {
        animate = false;
    }

    var statusfilter = $("#filter-buttons").find(".active").val();

    if (filter === "") {
        animate ? $(".order-list").find(".deliveryLibrary").parent().slideDown() : $(".order-list").find(".deliveryLibrary").parent().show();
        if (statusfilter != "") {
            //animate ? $(".order-list").find(".order-item-status:not(" + statusfilter + ")").parent().parent().slideUp() :
            $(".order-list").find(".order-item-status:not(" + statusfilter + ")").parent().parent().hide();
            // animate ? $(".order-list").find(".order-item-status" + statusfilter).parent().parent().slideDown() : $(".order-list").find(".order-item-status" + statusfilter).parent().parent().show();
        }
    }
    else {
        animate ? $(".order-list").find(".deliveryLibrary:not(" + filter + ")").parent().slideUp() : $(".order-list").find(".deliveryLibrary:not(" + filter + ")").parent().hide();
        animate ? $(".order-list").find(".deliveryLibrary" + filter).parent().slideDown() : $(".order-list").find(".deliveryLibrary" + filter).parent().show();
        if (statusfilter != ""){
            //animate ? $(".order-list").find(".order-item-status:not(" + statusfilter + ")").parent().parent().slideUp() :
            $(".order-list").find(".order-item-status:not(" + statusfilter + ")").parent().parent().hide();
            // animate ? $(".order-list").find(".order-item-status" + statusfilter).parent().parent().slideDown() : $(".order-list").find(".order-item-status" + statusfilter).parent().parent().show();
        }
    }
    updateFilterButtonCounters();
}

function updateFilterButtonCounters()
{
    var numberOfStatuses = 11;

    // TODO: AAAAAHHHH!! MY EYES!!! Rewrite this method.
    if ($("#library01-button").hasClass("active")) {
        // hbib
        $("#order-list-title").text("Best\u00E4llningar - Huvudbiblioteket");
        setCounterOrHide($("#status00-counter"), $(".order-list").find(".illedit > .Huvudbiblioteket").length);
        for (k = 1; k < (numberOfStatuses + 1); k++) {
            setCounterOrHide($("#status" + zeroPadFromLeft(k, 2) + "-counter"), $(".order-list").find("div > div > .status-" + zeroPadFromLeft(k, 2)).filter(function (item) {
                return $(this).parent().parent().find(".Huvudbiblioteket").length > 0;
            }).length);
        }
    } else if ($("#library02-button").hasClass("active")) {
        // lbib
        $("#order-list-title").text("Best\u00E4llningar - Lindholmenbiblioteket");
        setCounterOrHide($("#status00-counter"), $(".order-list").find(".illedit > .Lindholmenbiblioteket").length);
        for (k = 1; k < (numberOfStatuses + 1) ; k++) {
            setCounterOrHide($("#status" + zeroPadFromLeft(k, 2) + "-counter"), $(".order-list").find("div > div > .status-" + zeroPadFromLeft(k, 2)).filter(function (item) {
                return $(this).parent().parent().find(".Lindholmenbiblioteket").length > 0;
            }).length);
        }
    } else if ($("#library03-button").hasClass("active")) {
        // abib
        $("#order-list-title").text("Best\u00E4llningar - Arkitekturbiblioteket");
        setCounterOrHide($("#status00-counter"), $(".order-list").find(".illedit > .Arkitekturbiblioteket").length);
        for (k = 1; k < (numberOfStatuses + 1) ; k++) {
            setCounterOrHide($("#status" + zeroPadFromLeft(k, 2) + "-counter"), $(".order-list").find("div > div > .status-" + zeroPadFromLeft(k, 2)).filter(function (item) {
                return $(this).parent().parent().find(".Arkitekturbiblioteket").length > 0;
            }).length);
        }
    } else {
        // all
        $("#order-list-title").text("Best\u00E4llningar - Alla bibliotek");
        setCounterOrHide($("#status00-counter"), $(".order-list").find(".order-item-status").length) ;
        for (k = 1; k < (numberOfStatuses + 1) ; k++) {
            setCounterOrHide($("#status" + zeroPadFromLeft(k, 2) + "-counter"), $(".order-list").find(".status-" + zeroPadFromLeft(k, 2)).length);
        }
    }
}

function updateLibraryFilterButtonCounters()
{
    $("#allaBibliotek").text($(".order-list").find(".deliveryLibrary").length.toString());
    $("#Huvudbiblioteket").text($(".order-list").find(".Huvudbiblioteket").length.toString());
    $("#Lindholmenbiblioteket").text($(".order-list").find(".Lindholmenbiblioteket").length.toString());
    $("#Arkitekturbiblioteket").text($(".order-list").find(".Arkitekturbiblioteket").length.toString());
}

function setCounterOrHide(elem, count) {
    elem.text(count.toString());
}

// Load OrderItem Summary (first row in list)
function loadOrderItemSummary(id)
{
    $.getJSON("/umbraco/surface/OrderItemSurface/GetOrderItem?nodeId=" + id, function (json) {
        if (json.NodeId && $("#" + json.NodeId).length > 0) {

            // Update follow up date.
            // TODO: Fix culture sensitive date string creation.
            var followUpDate = new Date(parseInt(json.FollowUpDate.toString().match(/Date\(([0-9]*)\)/)[1]));
            $("#" + json.NodeId + " div[data-column='createDate']").text(followUpDate.getFullYear() + "-" + ("00" + (followUpDate.getMonth() + 1)).substr(-2) + "-" + ("00" + followUpDate.getDate()).substr(-2));

            // Update type
            $("#" + json.NodeId + " div[data-column='type']").text(json.TypePrevalue);

            // Update delivery library
            var delLibDiv = $("#" + json.NodeId + " div[data-column='deliveryLibrary']");
            delLibDiv.text(json.DeliveryLibraryPrevalue);

            // TODO: Should solve this in some less hard coded way.
            delLibDiv.removeClass("Huvudbiblioteket");
            delLibDiv.removeClass("Arkitekturbiblioteket");
            delLibDiv.removeClass("Lindholmenbiblioteket");
            delLibDiv.addClass(json.DeliveryLibraryPrevalue);


            // Update status
            var isOpen = $("#" + json.NodeId).hasClass("open");
            var statusClassAttr = $("#" + json.NodeId + " .order-item-status").attr("class");
            var statusClass = "";           

            if (statusClassAttr != null) {
                statusClass = statusClassAttr.match(/chillin-status-[0-9]{2}/)[0];
            }
            if (!$("#" + json.NodeId).hasClass("open") || statusClass === "") {
                statusClass = "chillin-status-" + json.StatusPrevalue.substring(0, 2);
            }
            if (json.StatusPrevalue.indexOf("01") == 0) {
                $("#" + json.NodeId + " div[data-column='status']").html("<span class=\"order-item-status label label-success status-" + json.StatusPrevalue.substring(0, 2) + " " + statusClass + "\">" + json.StatusString + "</span>");
            }
            else if (json.StatusPrevalue.indexOf("05") == 0 || 
                     json.StatusPrevalue.indexOf("06") == 0 || 
                     json.StatusPrevalue.indexOf("07") == 0 || 
                     json.StatusPrevalue.indexOf("08") == 0 ||
                     json.StatusPrevalue.indexOf("10") == 0 ||
                     json.StatusPrevalue.indexOf("11") == 0) {
                $("#" + json.NodeId + " div[data-column='status']").html("<span class=\"order-item-status label label-info status-" + json.StatusPrevalue.substring(0, 2) + " " + statusClass + "\">" + json.StatusString + "</span>");
            }
            else {
                $("#" + json.NodeId + " div[data-column='status']").html("<span class=\"order-item-status label label-danger status-" + json.StatusPrevalue.substring(0, 2) + " " + statusClass + "\">" + json.StatusString + "</span>");
            }

            // Reference with links
            $("#" + json.NodeId + " div[data-column='reference']").html(replaceURLWithHTMLLinks(json.Reference));

            // Mark as locked if being edited by other member
            if (json.EditedBy != "" && json.EditedByCurrentMember == false) {
                $("#" + json.NodeId).addClass("locked");
            }

            // Remove locked mark if noone is editing
            else if (json.EditedBy == "" && $("#" + json.NodeId).hasClass("locked")) {
                $("#" + json.NodeId).removeClass("locked");
            }

            reapplyFiltersAndUpdateCounters();
        }
    });
}

/* Set the STATUS of an OrderItem, reload if OK and display alert if ERROR */

function setOrderItemStatus(node, status) {
    lockScreen();
    $.getJSON("/umbraco/surface/OrderItemStatusSurface/SetOrderItemStatus?orderNodeId=" + node + "&statusId=" + status, function (json) {
        if (json.Success) {
            loadOrderItemDetails(node);
        }
        else {
            alert(json.Message);
        }
        unlockScreen();
    }).fail(function (jqxhr, textStatus, error) {
        alert("Error: " + textStatus + " " + error);
        unlockScreen();
    });
}

function setOrderItemStatusAndCancellationReason(node, status, reason) {
    lockScreen();
    $.getJSON("/umbraco/surface/OrderItemStatusSurface/SetOrderItemStatus?orderNodeId=" + node + "&statusId=" + status, function (json) {
        if (json.Success) {
            $.getJSON("/umbraco/surface/OrderItemCancellationReasonSurface/SetOrderItemCancellationReason?orderNodeId=" + node + "&cancellationReasonId=" + reason, function (json) {
                if (json.Success) {
                    loadOrderItemDetails(node);
                }
                else {
                    alert(json.Message);
                }
                unlockScreen();
            }).fail(function (jqxhr, textStatus, error) {
                alert("Error: " + textStatus + " " + error);
                unlockScreen();
            });
        }
        else {
            alert(json.Message);
            unlockScreen();
        }
    }).fail(function (jqxhr, textStatus, error) {
        alert("Error: " + textStatus + " " + error);
        unlockScreen();
    });
}

function setOrderItemStatusAndPurchasedMaterial(node, status, material) {
    lockScreen();
    $.getJSON("/umbraco/surface/OrderItemStatusSurface/SetOrderItemStatus?orderNodeId=" + node + "&statusId=" + status, function (json) {
        if (json.Success) {
            $.getJSON("/umbraco/surface/OrderItemPurchasedMaterialSurface/SetOrderItemPurchasedMaterial?orderNodeId=" + node + "&purchasedMaterialId=" + material, function (json) {
                if (json.Success) {
                    loadOrderItemDetails(node);
                }
                else {
                    alert(json.Message);
                }
                unlockScreen();
            }).fail(function (jqxhr, textStatus, error) {
                alert("Error: " + textStatus + " " + error);
                unlockScreen();
            });
        }
        else {
            alert(json.Message);
            unlockScreen();
        }
    }).fail(function (jqxhr, textStatus, error) {
        alert("Error: " + textStatus + " " + error);
        unlockScreen();
    });
}

/* Set the TYPE of an OrderItem, reload if OK and display alert if ERROR */

function setOrderItemType(node, type) {
    lockScreen();
    $.getJSON("/umbraco/surface/OrderItemTypeSurface/SetOrderItemType?orderNodeId=" + node + "&typeId=" + type, function (json) {
        if (json.Success) {
            loadOrderItemDetails(node);
        }
        else {
            alert(json.Message);
        }
        unlockScreen();
    }).error(unlockScreen);
}

function setOrderItemDeliveryLibrary(node, deliveryLibrary) {
    lockScreen();
    $.getJSON("/umbraco/surface/OrderItemDeliveryLibrarySurface/SetOrderItemDeliveryLibrary?orderNodeId=" + node + "&deliveryLibraryId=" + deliveryLibrary, function (json) {
        if (json.Success) {
            loadOrderItemDetails(node);
        }
        else {
            alert(json.Message);
        }
        unlockScreen();
    }).error(unlockScreen);
}

function setOrderItemDeliveryReceived(node, bookId, dueDate, providerInformation, maildata, logMsg) {
    lockScreen();
    $.post("/umbraco/surface/OrderItemDeliverySurface/SetOrderItemDeliveryReceived", {
        packJson: JSON.stringify({
            orderNodeId: node,
            bookId: bookId,
            dueDate: dueDate,
            providerInformation: providerInformation,
            mailData: maildata,
            logMsg: logMsg
        })
    }, function (json) {
        if (json.Success) {
            loadOrderItemDetails(node);
        }
        else {
            alert(json.Message);
        }
        unlockScreen();
    }).error(unlockScreen);
}

function loadLogItems(id)
{
    $.getJSON("/umbraco/surface/LogItemSurface/GetLogItems?nodeid="+id, function (data) {
        $.each(data, function (item) {
            $('#log-' + id).append("<div class=\"row log-item\"><div class=\"col-sm-3\"><span class=\"log-createdate\">" + data[item].CreateDate + "</span> <span class=\"log-membername\">" + data[item].MemberName + "</span></div><div class=\"col-sm-1\"><span class=\"log-type\">" + data[item].Type + "</div><div class=\"col-sm-6\"><span class=\"log-message\">" + replaceURLWithHTMLLinks(data[item].Message) + "</span></div></div>");
        });
    });
}

/* Set new property values for Provider from form */

function setOrderItemProvider(nodeId, providerName, providerOrderId, followUpDate)
{
    lockScreen();
    $.getJSON("/umbraco/surface/OrderItemProviderSurface/SetProvider?nodeId=" + nodeId + "&providerName=" + providerName + "&providerOrderId=" + providerOrderId.trim() + "&newFollowUpDate=" + followUpDate, function (json) {
        if (json.Success) {
            loadOrderItemDetails(nodeId);
        }
        else {
            alert(json.Message);
        }
        unlockScreen();
    }).error(unlockScreen);
}

/* Set new property values for Reference from form */

function setOrderItemReference(nodeId, reference) {
    lockScreen();
    $.post("/umbraco/surface/OrderItemReferenceSurface/SetReference", { nodeId: nodeId, reference: reference }).done(function (json) {
        if (json.Success) {
            loadOrderItemDetails(nodeId);
        }
        else {
            alert(json.Message);
        }
        unlockScreen();
    }).error(unlockScreen);
}

function sendDeliveryByEmail(mailData, logEntry) {
    lockScreen();
    if (mailData.message && mailData.recipientEmail) {
        $.ajax({
            type: "POST",
            url: "/umbraco/surface/OrderItemMailSurface/SendMail",
            data: JSON.stringify(mailData),
            success: function (json) {
                if (json.Success) {
                    $.post("/umbraco/surface/OrderItemDeliverySurface/SetDelivery", {
                        nodeId: mailData.nodeId,
                        logEntry: logEntry,
                        delivery: "Direktleverans via e-post"
                    }, function (json) {
                        if (json.Success) {
                            loadOrderItemDetails(mailData.nodeId);
                        }
                        else {
                            alert(json.Message);
                        }
                        unlockScreen();
                    }).error(unlockScreen);
                }
                else
                {
                    alert(json.Message);
                    unlockScreen();
                }
            },
            error: function (jqxhr, textStatus, errorThrown) {
                alert(textStatus + "\n" + errorThrown);
                unlockScreen();
            },
            contentType: "application/json"
        });
}
    else {
        if (mailData.message == "") {
            alert("Du m\u00E5ste skriva ett meddelande till mottagaren.");
        }
        if (mailData.recipientEmail == "") {
            alert("Du m\u00E5ste ange en mottagande e-postadress.");
        }
        unlockScreen();
    }
}

/* Set new property values for Delivery from form */
function setOrderItemDelivery(nodeId, logEntry, delivery) {
    lockScreen();
    $.post("/umbraco/surface/OrderItemDeliverySurface/SetDelivery", {
        nodeId: nodeId,
        logEntry: logEntry,
        delivery: delivery
    }, function (json) {
        if (json.Success) {
            loadOrderItemDetails(nodeId);
        }
        else {
            alert(json.Message);
        }
        unlockScreen();
    }).error(unlockScreen);
}

function sendMailForNewOrder(body, name, mail, libCardNr) {
    lockScreen();
    if (body && name && mail && libCardNr) {
        $.post("/umbraco/surface/OrderItemMailSurface/SendMailForNewOrder", { message: body, name: name, mail: mail, libraryCardNumber: libCardNr }).done(function (json) {
            if (json.Success) {
                alert("Successfully sent new order!");
            }
            else {
                alert(json.Message);
            }
            unlockScreen();
        }).fail(function (jqxhr, textStatus, error) {
            alert("Error: " + textStatus + " " + error);
            unlockScreen();
        });
    }
    else {
        if (body == "") {
            alert("Du m\u00E5ste skriva ett meddelande.");
        }
        if (name == "") {
            alert("Du m\u00E5ste skriva ditt namn.");
        }
        if (mail == "") {
            alert("Du m\u00E5ste ange din e-postadress.");
        }
        if (libCardNr == "") {
            alert("Du m\u00E5ste skriva ditt bibliotekskortsnummer.");
        }

        unlockScreen();
    }
}

/* Send mail to Patron */
function sendMailToPatron(mailData) {
    lockScreen();
    if (message && recipientEmail) {
        $.post("/umbraco/surface/OrderItemMailSurface/SendMail", mailData).done(function (json) {
            if (json.Success) {
                loadOrderItemDetails(mailData.nodeId);
            }
            else {
                alert(json.Message);
            }
            unlockScreen();
        }).error(unlockScreen);
    }
    else {
        if (message == "") {
            alert("Du m\u00E5ste skriva ett meddelande till mottagaren.");
        }
        if (recipientEmail == "") {
            alert("Du m\u00E5ste ange en mottagande e-postadress.");
        }
        unlockScreen();
    }
}

/* Write Log Entry */
function writeLogItem(nodeId, message, type, followUpDate, shouldUnlockScreen) {
    lockScreen();
    shouldUnlockScreen = typeof shouldUnlockScreen !== "undefined" ? shouldUnlockScreen : true;
    if (message) {
        $.post("/umbraco/surface/LogItemSurface/WriteLogItem", { nodeId: nodeId, Message: message, Type: type, newFollowUpDate: followUpDate }).done(function (json) {
            if (json.Success) {
                loadOrderItemDetails(nodeId);
            }
            else {
                alert(json.Message);
            }
            if (shouldUnlockScreen) unlockScreen();
        }).error(function () {
            if (shouldUnlockScreen) unlockScreen();
        });
    }
    else {
        alert("Du m\u00E5ste skriva n\u00E5got.");
        if (shouldUnlockScreen) unlockScreen();
    }
}

function fetchDataFromSierraUsingLibraryCardNumber(orderItemNodeId, lcn) {
    lockScreen();
    $.post("/umbraco/surface/OrderItemPatronDataSurface/FetchPatronDataUsingLcn", { orderItemNodeId: orderItemNodeId, lcn: lcn }).done(function (json) {
        if (json.Success) {
            loadOrderItemDetails(orderItemNodeId);
        } else {
            alert(json.Message);
        }
        unlockScreen();
    });
}

/* Load Partial View with form for setting Provider details */
function loadProviderAction(id) {
    $("#loading-partial-view").show();
    $('#action-' + id).html("").show().load('/umbraco/surface/OrderItemProviderSurface/RenderProviderAction?nodeId=' + id,
        function (responseText, textStatus, req) {
            if (req.status == 200) {
                $('#action-' + id + ' #providerName').focus();
            }
            $("#loading-partial-view").hide();
        }
    );
}

/* Load Partial View for the Reference Action */
function loadReferenceAction(id) {
    $("#loading-partial-view").show();
    $('#action-' + id).html("").show().load('/umbraco/surface/OrderItemReferenceSurface/RenderReferenceAction?nodeId=' + id,
        function (responseText, textStatus, req) {
            if (req.status == 200) {
                $('#action-' + id + ' #reference').focus();
            }
            $("#loading-partial-view").hide();
        }
    );
}

/* Load Partial View for the Mail to Patron Action */
function loadMailAction(id) {
    $("#loading-partial-view").show();
    $('#action-' + id).html("").show().load('/umbraco/surface/OrderItemMailSurface/RenderMailAction?nodeId=' + id,
        function (responseText, textStatus, req) {
            if (req.status == 200) {
                $('#action-' + id + ' #message').focus();
            }
            $("#loading-partial-view").hide();
        }
    );
}

/* Load Partial View for the Logging Action */
function loadLogEntryAction(id) {
    $("#loading-partial-view").show();
    $('#action-' + id).html("").show().load('/umbraco/surface/LogItemSurface/RenderLogEntryAction?nodeId=' + id,
        function (responseText, textStatus, req) {
            if (req.status == 200) {
                $('#action-' + id + ' #message').focus();
            }
            $("#loading-partial-view").hide();
        }
    );
}

/* Load Partial View for the Delivery Action */
function loadDeliveryAction(id) {
    $("#loading-partial-view").show();
    $('#action-' + id).html("").show().load('/umbraco/surface/OrderItemDeliverySurface/RenderDeliveryAction?nodeId=' + id,
        function (responseText, textStatus, req) {
            if (req.status == 200) {
                $('#action-' + id + ' #radio').focus();
            }
            $("#loading-partial-view").hide();
        }
    );
}

function loadClaimAction(id) {
    $("#loading-partial-view").show();
    $('#action-' + id).html("").show().load('/umbraco/surface/OrderItemClaimSurface/RenderClaimAction?nodeId=' + id,
        function (responseText, textStatus, req) {
            $("#loading-partial-view").hide();
        }
    );
}

function loadProviderReturnDateAction(id) {
    $("#loading-partial-view").show();
    $('#action-' + id).html("").show().load('/umbraco/surface/OrderItemProviderReturnDateSurface/RenderProviderReturnDateAction?nodeId=' + id,
        function (responseText, textStatus, req) {
            $("#loading-partial-view").hide();
        }
    );
}

function loadPatronReturnDateAction(id) {
    $("#loading-partial-view").show();
    $('#action-' + id).html("").show().load('/umbraco/surface/OrderItemPatronReturnDateSurface/RenderPatronReturnDateAction?nodeId=' + id,
        function (responseText, textStatus, req) {
            $("#loading-partial-view").hide();
        }
    );
}

function loadReturnAction(id) {
    $("#loading-partial-view").show();
    $('#action-' + id).html("").show().load('/umbraco/surface/OrderItemReturnSurface/RenderReturnAction?nodeId=' + id,
        function (responseText, textStatus, req) {
            $("#loading-partial-view").hide();
        }
    );
}

function loadPatronDataView(id) {
    $("#loading-partial-view").show();
    $('#action-' + id).html("").show().load('/umbraco/surface/OrderItemPatronDataSurface/RenderPatronDataView?nodeId=' + id,
        function (responseText, textStatus, req) {
            if (req.status == 200) {
                // NOP
            }
            $("#loading-partial-view").hide();
        }
    );
}

/* SIGNALR RELATED */
var notifier = $.connection.notificationHub;

// Message from SignalR called updateStream when node has been Saved
notifier.client.updateStream = function (value) {
    // Check if the node which is signaled is open here.
    if (value.UpdateFromMail && $("#edit-" + value.NodeId).length > 0)
    {
        if (confirm("Ordern som \u00E4r \u00F6ppen har uppdaterats pga. mottaget mail, vill du ladda om den?"))
        {
            loadOrderItemDetails(value.NodeId);
        }
    }

    // If this NodeId is in current member's list, always load the summary in case values changed
    if ($("#" + value.NodeId).length > 0) {
        // Only update if we haven't got -1 as EditedBy, -1 means that we haven't checked edited by properly and should disregard it
        if (value.EditedBy != -1) {
            loadOrderItemSummary(value.NodeId);
        }
    } else {
        // We do not have the order item in our list, show this to the user.
        var pendingOrderCountBefore = parseInt($("#pending-order-counter").text());
        var pendingOrderItems = $("#pending-order-counter").data("pending-order-items");
        if (value.SignificantUpdate && value.IsPending && pendingOrderItems.indexOf(value.NodeId) == -1)
        {
            // Something new needs our attention.
            pendingOrderItems.push(value.NodeId);
            $("#pending-order-counter").data("pending-order-items", pendingOrderItems);
            $("#pending-order-counter").text(parseInt($("#pending-order-counter").text()) + 1);
        }
        else if (value.SignificantUpdate && !value.IsPending && pendingOrderItems.indexOf(value.NodeId) > -1)
        {
            // Something that was new doesn't need our attention any longer.
            pendingOrderItems.splice(pendingOrderItems.indexOf(value.NodeId), 1);
            $("#pending-order-counter").data("pending-order-items", pendingOrderItems);
            $("#pending-order-counter").text(parseInt($("#pending-order-counter").text()) - 1);
        }
        var pendingOrderCountAfter = parseInt($("#pending-order-counter").text());

        if (pendingOrderCountBefore == 0 && pendingOrderCountAfter > 0)
        {
            $("#orders-button > a").addClass("pending-orders");
        } 
        else if (pendingOrderCountBefore > 0 && pendingOrderCountAfter == 0)
        {
            $("#orders-button > a").removeClass("pending-orders");
        }
    }

    // If current member has NodeId in open state and SignalR says other member has taken lock
    if ($("#" + value.NodeId).hasClass("open") && value.EditedBy != -1) {
        if ($("#action-buttons-" + value.NodeId).attr("data-locked-by-memberid")) {
            if (value.EditedBy != "" && $("#action-buttons-" + value.NodeId).attr("data-locked-by-memberid") != value.EditedBy) {
                alert(value.EditedByMemberName + " (" + value.EditedBy + ") took lock from you (" + $("#action-buttons-" + value.NodeId).attr("data-locked-by-memberid") + ").");
                loadOrderItemDetails(value.NodeId);
                $("#"+value.NodeId).removeAttr("data-locked-by-memberid");
            }
        }
    }

    // Add to debug messages
    $("#debug-bucket .panel-body").prepend("<div>NodeId=" + value.NodeId + ", EditedBy=" + value.EditedBy + ", EditedByMemberName=" + value.EditedByMemberName + "</div>");
};

/* Starting the signalR hub connections */
$.connection.hub.start()
    .done(function () {
        $("#debug-bucket .panel-body").prepend('Connected to signalR notification hub');
    })
    .fail(function () {
        alert("Could not Connect to signalR notification hub");
    });

/* Small helper functions */

function zeroPadFromLeft(num, size) {
    var s = "000000000" + num;
    return s.substr(s.length - size);
}

function openDocument(btn) {
    var win = window.open($(btn).data("link"), "_blank");
    if (win) {
        //Browser has allowed it to be opened
        win.focus();
    } else {
        //Browser has blocked it
        alert("Misslyckades med att öppna popup-fönster.");
    }
}