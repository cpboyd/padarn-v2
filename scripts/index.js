const fs = require('fs');
const _ = require('lodash');
const cheerio = require('cheerio');
const rp = require('request-promise');

const mdnRoot = 'https://developer.mozilla.org';
const elementPath = '/en-US/docs/Web/HTML/Element';

/** Used to map characters to HTML entities. */
const htmlEscapes = {
  '&': '&amp;',
  '<': '&lt;',
  '>': '&gt;',
  '"': '&quot;',
  "'": '&#39;'
};

/**
 * Used by `escape` to convert characters to HTML entities.
 *
 * @private
 * @param {string} chr The matched character to escape.
 * @returns {string} Returns the escaped character.
 */
function escapeHtmlChar(chr) {
    return htmlEscapes[chr];
}

/** Used to match "<" and ">" characters. */
const reUnescapedHtml = /[<>]/g,
    reHasUnescapedHtml = RegExp(reUnescapedHtml.source);

/**
 * Converts the characters "<" and ">" in `string` to their
 * corresponding HTML entities.
 *
 * @static
 * @category String
 * @param {string} [string=''] The string to escape.
 * @returns {string} Returns the escaped string.
 * @example
 *
 * escape('<test>');
 * // => '&lt;test&gt;'
 */
function escape(string) {
    string = _.toString(string);
    return (string && reHasUnescapedHtml.test(string))
        ? string.replace(reUnescapedHtml, escapeHtmlChar)
        : string;
}

function getElementType(str) {
    if (str.includes('obsolete')) {
        return 'obsolete';
    }
    if (str.includes('deprecated')) {
        return 'deprecated';
    }
    if (str.includes('experimental')) {
        return 'experimental';
    }
}

function getTypeFromNode($, node) {
    const titles = node.find('span[title]')
        .map(function(i, el) {
            // this === el
            const str = $(this).attr('title').toLowerCase();
            return getElementType(str);
        })
        .get();
        
    return getElementType(titles);
}

function indent(array, indent) {
    indent = indent || '    ';

    if (!Array.isArray(array)) {
        return indent + array;
    }
    
    return array.map(x => indent + x);
}

function writeLine(stream, str, indent, softBreak) {
    indent = indent || '';
    softBreak = softBreak || 100;
    const output = indent + str;
    const index = output.indexOf(' ', softBreak);
    if (index < 0) {
        stream.write(output + '\r\n');
        return;
    }

    const start = output.substring(0, index);
    stream.write(start + '\r\n');

    const hasComment = start.match(/\/\/+/);
    const comment = hasComment ? hasComment[0] : '';
    const startIndent = start.match(/^\s+/);
    const end = comment + output.substring(index);
    writeLine(stream, end, startIndent ? startIndent[0] : '');
}

function writeLines(stream, array, indent) {
    indent = indent || '';

    if (!Array.isArray(array)) {
        writeLine(stream, array, indent);
    }

    for (const str of array) {
        writeLine(stream, str, indent);
    }
}

function getEnumValue(node) {
    // Don't insert nodes without a name:
    if (!node || !node.name) {
        return '';
    }

    const name = _.capitalize(node.name.replace(/\W/g, ''));
    return [
        ...getLinkedSummary(node),
        name + ',',
        '',
    ]
}

function getLinkedSummary(node) {
    const summary = node && node.summary || '';

    const index = summary.indexOf(node.name);
    if (index < 0) {
        return getSummary(escape(summary));
    }
    
    // TODO: use <see> when referencing other elements?
    var linkedSummary = escape(summary.substring(0, index)) +
        '<a href="' + mdnRoot + node.link + '">' +
        escape(node.name) +
        '</a>' +
        escape(summary.substring(index + node.name.length));
    return getSummary(linkedSummary);
}

function getSummary(summary) {
    return [
        '/// <summary>',
        '/// ' + summary,
        '/// </summary>',
    ];
}

function writeSummary(stream, summary, indent) {
    indent = indent || '';
    stream.write(indent + '/// <summary>\r\n' +
        indent + '/// ' + summary + '\r\n' +
        indent + '/// </summary>\r\n');
}

async function main() {
    const response = await rp(mdnRoot + elementPath);
    const $ = cheerio.load(response);

    const elements = $('a[href="' + elementPath + '"]')
        .next('ol')
        .children('li')
        .map(function(i, el) {
            // this === el
            const node = $(this);
            const link = node.children('a');
            return {
                name: node.text(),
                summary: link.attr('title'),
                link: link.attr('href'),
                type: getTypeFromNode($, node),
            };
        })
        .get();

    // TODO: Create extra enum for non-standard elements?
    const obsolete = _.remove(elements, x => x.type === 'obsolete');
    const deprecated = _.remove(elements, x => x.type === 'deprecated');
    const experimental = _.remove(elements, x => x.type === 'experimental');

    const stream = fs.createWriteStream('HtmlTextWriterTag.cs');
    
    elements.unshift({
        name: 'Unknown',
        summary: 'The string passed as an HTML tag is not recognized.'
    });

    // Only <h1> is listed for all heading elements, need to add the rest:
    const h1Index = elements.findIndex(x => x.name === '<h1>');
    var headings = [];
    for (var i = 2; i < 7; i++) {
        const name = `<h${i}>`;
        headings.push({
            name,
            summary: name,
            link: elements[h1Index].link,
            type: elements[h1Index].type,
        })
    }
    elements.splice(h1Index + 1, 0, ...headings);

    writeLines(stream, [
        '// Copyright ©2018 Christopher Boyd',
        '//',
        '// Generated by mdn-parser on ' + new Date().toISOString().split('T')[0],
        '',
        'namespace OpenNETCF.Web.UI',
        '{',
        ...indent([
            ...getSummary('Specifies the HTML tags that can be passed to an HtmlTextWriter object output stream.'),
            'public enum HtmlTextWriterTag',
            '{',
            ...indent(_.flatMap(elements, x => getEnumValue(x))),
            '}',
        ]),
        '}',
    ]);
    
}

main();