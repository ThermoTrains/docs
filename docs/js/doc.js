(function ($) {
  "use strict";

  var Document = function () {

  };

  Document.prototype.render = function () {
    this.renderTableOfContents();
    this.renderTableOfFigures();
    this.renderBibliography();
  };

  Document.prototype.renderTableOfContents = function () {
    var headers = $('h2');
    var toc = $('#toc');

    // skip first two headers (Abstract, TOC)
    headers = headers.slice(2);

    headers.each(function (i, element) {
      element = $(element);

      i++;
      var id = 'chapter-' + i;
      var li = createListItem(element, id, i);

      var children = element.closest('.chapter').find('h3');
      var ol = $('<ol>');

      children.each(function (j, child) {

        child = $(child);

        j++;
        id = 'chapter-' + i + '-' + j;
        var sli = createListItem(child, id, i, j);
        ol.append(sli);

      });

      li.append(ol);
      toc.append(li);
    });
  };

  Document.prototype.renderTableOfFigures = function () {
    var captions = $('figcaption');
    var figures = $('#figures');

    captions.each(function (i, caption) {

      caption = $(caption);
      var figure = caption.parents('figure');
      var id = 'figure-' + i;

      figure.attr('id', id);
      caption.prepend('<span>Fig. ' + (i + 1) + ' </span>');

      var li = $('<li>');
      var a = $('<a>').attr('href', '#' + id).text(caption.text());

      li.append(a);
      figures.append(li);
    });
  };

  Document.prototype.renderBibliography = function () {
    var references = $('.reference-item');
    var bibliography = $('#references');

    references.each(function (i, reference) {
      bibliography.append($(reference));
      // reference.remove();
    });
  };

  function createListItem(node, id, chapter, subChapter) {
    node.attr('id', id);

    var li = $('<li>');
    var a = $('<a>').attr('href', '#' + id);

    chapter = '' + chapter;
    if (typeof subChapter != 'undefined') {
      chapter += '.' + subChapter;
    }

    $('<span>').appendTo(a).attr('class', 'toc-nr').text(chapter);
    $('<span>').appendTo(a).attr('class', 'toc-title').text(node.text());
    li.append(a);

    return li;
  }

  function refFootnote(href) {
    var reference = $(href);
    var link = reference.find('a');
    var ref = reference.find('.ref');

    return link.length > 0 ? link.text() : ref.text();
  }

  if (typeof Prince != 'undefined') {
    Prince.addScriptFunc('ref-footnote', refFootnote);
  }

  $(function () {
    var doc = new Document();
    doc.render();
  });

})(jQuery);
