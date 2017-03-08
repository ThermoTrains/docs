var doc = doc || {};

(function ($) {
    "use strict";

    doc.Document = function () {

    };

    doc.Document.prototype.render = function () {
        this.renderTableOfContents();
        this.renderTableOfFigures();
    };

    doc.Document.prototype.renderTableOfContents = function () {

        var headers = $('h2'),
            toc = $('#toc'),
            i, j, node, id, chapter, li, sli, childs, ol;

        // skip first two headers (Abstract, TOC)
        headers = headers.slice(2);

        headers.each(function (i, element) {

            element = $(element);

            i = i + 1;
            id = 'chapter-' + i;
            li = doc.createListItem(element, id, i);

            childs = element.closest('.chapter').find('h3');
            ol = $('<ol>');

            childs.each(function (j, child) {

                child = $(child);

                j = j + 1;
                id = 'chapter-' + i + '-' + j;
                sli = doc.createListItem(child, id, i, j);
                ol.append(sli);

            });

            li.append(ol);
            toc.append(li);

        });
    };

    doc.createListItem = function (node, id, chapter, subChapter) {

        var li, a, spanChapter, spanTitle;

        node.attr('id', id);

        li = $('<li>');

        a = $('<a>').attr('href', '#' + id);

        chapter = '' + chapter;
        if (typeof subChapter != 'undefined') {
            chapter += '.' + subChapter;
        }

        $('<span>').appendTo(a).attr('class', 'toc-nr').text(chapter);

        spanTitle = $('span');
        $('<span>').appendTo(a).attr('class', 'toc-title').text(node.text());

        li.append(a);

        return li;
    };

    doc.Document.prototype.renderTableOfFigures = function () {

        var captions = $('figcaption'),
            figures = $('#figures'),
            figure, li, a, id;

        captions.each(function (i, caption) {

            caption = $(caption);
            figure = caption.parents('figure');
            id = 'figure-' + i;

            figure.attr('id', id);
            caption.prepend('<span>Fig. ' + (i + 1) + ' </span>');

            li = $('<li>');

            a = $('<a>').attr('href', '#' + id).text(caption.text());

            li.append(a);
            figures.append(li);
        });
    };

    doc.refFootnote = function (href) {

        var reference = $(href),
            link = reference.find('a'),
            ref = reference.find('.ref'),
            footnote;

        if (link.length > 0) {
            footnote = link.text();
        } else {
            footnote = ref.text();
        }

        return footnote;
    };

    if (typeof Prince != 'undefined') {
        Prince.addScriptFunc('ref-footnote', doc.refFootnote);
    }

    $(function () {

        var document = new doc.Document();
        document.render();

    });

})(jQuery);
