// Web sözlüğünün kullanıcı bölümlerini (src/LifeQuest.Web/src/app/core/i18n/sections) mobil için C#'a çevirir.
// Çıktı: src/LifeQuest.Mobile.Core/Localization/Strings.g.cs — her metin iki dili yan yana taşır: T("tr", "en").
// Çalıştırma: node tools/mobile/gen-strings.mjs   (web bağımlılıkları kurulu olmalı: typescript paketi)
import { createRequire } from 'node:module';
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const require = createRequire(join(root, 'src/LifeQuest.Web/package.json'));
const ts = require('typescript');

const SECTIONS = ['common', 'labels', 'ui', 'auth', 'onboarding', 'today', 'quests', 'quest', 'party', 'pages', 'profile'];
const dir = join(root, 'src/LifeQuest.Web/src/app/core/i18n/sections');

/** JS koşullu ifadelerinin elle yazılmış C# karşılıkları (otomatik çeviriye uymayanlar). */
const OVERRIDES = {
  "name ? ', ' + name : ''": '(string.IsNullOrEmpty(name) ? "" : ", " + name)',
  "name ? ' ' + name : ''": '(string.IsNullOrEmpty(name) ? "" : " " + name)',
};

const pascal = (k) => {
  const s = k.replace(/^['"]|['"]$/g, '');
  if (/^\d/.test(s)) return 'M' + s;
  return s.charAt(0).toUpperCase() + s.slice(1);
};
const csString = (s) => '"' + s.replace(/\\/g, '\\\\').replace(/"/g, '\\"').replace(/\n/g, '\\n') + '"';

function propsOf(obj) {
  const map = new Map();
  for (const p of obj.properties) {
    if (!ts.isPropertyAssignment(p) && !ts.isMethodDeclaration(p)) throw new Error('unsupported member ' + p.getText());
    map.set(p.name.getText().replace(/^['"]|['"]$/g, ''), p);
  }
  return map;
}

function stripParens(node) {
  while (ts.isParenthesizedExpression(node) || ts.isAsExpression(node) || ts.isSatisfiesExpression?.(node)) node = node.expression;
  return node;
}

function jsExprToCs(text) {
  if (OVERRIDES[text]) return OVERRIDES[text];
  let s = text.replace(/===/g, '==').replace(/!==/g, '!=').replace(/'([^']*)'/g, (_, x) => csString(x));
  return /[?:+]/.test(s) ? `(${s})` : s;
}

function templateToCs(node, sf) {
  node = stripParens(node);
  if (ts.isNoSubstitutionTemplateLiteral(node) || ts.isStringLiteral(node)) return csString(node.text);
  if (ts.isTemplateExpression(node)) {
    const esc = (t) => t.replace(/\\/g, '\\\\').replace(/"/g, '\\"').replace(/\{/g, '{{').replace(/\}/g, '}}').replace(/\n/g, '\\n');
    let out = esc(node.head.text);
    for (const span of node.templateSpans) {
      out += '{' + jsExprToCs(span.expression.getText(sf)) + '}' + esc(span.literal.text);
    }
    return '$"' + out + '"';
  }
  if (ts.isConditionalExpression(node) || ts.isBinaryExpression(node)) return jsExprToCs(node.getText(sf));
  throw new Error('unsupported function body: ' + node.getText(sf));
}

function csType(typeNode) {
  const t = typeNode?.getText() ?? 'string';
  if (t === 'number') return 'int';
  if (t === 'string') return 'string';
  throw new Error('unsupported parameter type ' + t);
}

const classes = [];
const interfaces = [];
const implementsOf = new Map();
function emitClass(name, tr, en, sf) {
  const lines = [];
  const trProps = propsOf(tr), enProps = propsOf(en);
  for (const [key, trProp] of trProps) {
    const enProp = enProps.get(key);
    if (!enProp) throw new Error(`${name}.${key}: missing English`);
    const member = pascal(key);
    const trInit = stripParens(trProp.initializer), enInit = stripParens(enProp.initializer);
    if (ts.isStringLiteral(trInit) || ts.isNoSubstitutionTemplateLiteral(trInit)) {
      lines.push(`    public string ${member} => T(${csString(trInit.text)}, ${templateToCs(enInit, sf)});`);
    } else if (ts.isArrowFunction(trInit)) {
      const params = trInit.parameters.map((p) => `${csType(p.type)} ${p.name.getText(sf)}`).join(', ');
      lines.push(`    public string ${member}(${params}) => T(${templateToCs(trInit.body, sf)}, ${templateToCs(enInit.body, sf)});`);
    } else if (ts.isArrayLiteralExpression(trInit)) {
      const arr = (a) => 'new[] { ' + a.elements.map((e) => templateToCs(e, sf)).join(', ') + ' }';
      lines.push(`    public IReadOnlyList<string> ${member} => T(${arr(trInit)}, ${arr(enInit)});`);
    } else if (ts.isObjectLiteralExpression(trInit)) {
      const child = name + member;
      emitClass(child, trInit, enInit, sf);
      lines.push(`    public ${child}Strings ${member} { get; } = new();`);
    } else {
      throw new Error(`${name}.${key}: unsupported ${ts.SyntaxKind[trInit.kind]}`);
    }
  }
  // Yalnızca düz metinlerden oluşan bölümler (ör. enum → etiket) anahtarla da okunabilir: S.Labels.Categories["Explorer"].
  const plain = [...trProps].filter(([, p]) => {
    const i = stripParens(p.initializer);
    return ts.isStringLiteral(i) || ts.isNoSubstitutionTemplateLiteral(i);
  }).map(([k]) => k);
  // Aynı biçimli alt bölümler (ör. Cost.Free/Low/… hepsi short/label/hint) ortak arayüzle anahtardan okunur.
  const children = [...trProps].map(([k, p]) => [k, stripParens(p.initializer)]);
  const shapes = children.map(([, i]) => ts.isObjectLiteralExpression(i)
    ? i.properties.map((q) => q.name.getText(sf)).join(',') : null);
  if (children.length > 1 && shapes.every((x) => x !== null && x === shapes[0])) {
    const iface = `I${name}Item`;
    const members = children[0][1].properties.map((q) => {
      const init = stripParens(q.initializer);
      if (!(ts.isStringLiteral(init) || ts.isNoSubstitutionTemplateLiteral(init))) return null;
      return `    string ${pascal(q.name.getText(sf))} { get; }`;
    });
    if (members.every((m) => m !== null)) {
      interfaces.push(`public interface ${iface}\n{\n${members.join('\n')}\n}`);
      for (const [k] of children) implementsOf.set(name + pascal(k), iface);
      const arms = children.map(([k]) => `        ${csString(k)} => ${pascal(k)},`).join('\n');
      lines.push(`\n    public ${iface} this[string key] => key switch\n    {\n${arms}\n        _ => throw new KeyNotFoundException(key),\n    };`);
    }
  }
  if (plain.length === trProps.size && plain.length > 1) {
    const arms = plain.map((k) => `        ${csString(k)} => ${pascal(k)},`).join('\n');
    lines.push(`\n    public string this[string key] => key switch\n    {\n${arms}\n        _ => key,\n    };`);
  }
  classes.push({ name, body: lines.join('\n') });
}

const roots = [];
for (const file of SECTIONS) {
  const path = join(dir, file + '.ts');
  const sf = ts.createSourceFile(path, readFileSync(path, 'utf8'), ts.ScriptTarget.Latest, true);
  sf.forEachChild(function visit(node) {
    if (ts.isVariableDeclaration(node) && node.initializer && ts.isCallExpression(node.initializer)
        && node.initializer.expression.getText(sf) === 'section') {
      const [tr, en] = node.initializer.arguments.map(stripParens);
      const name = pascal(node.name.getText(sf));
      emitClass(name, tr, en, sf);
      roots.push(`    public ${name}Strings ${name} { get; } = new();`);
    }
    node.forEachChild(visit);
  });
}

const out = `// <auto-generated>
// tools/mobile/gen-strings.mjs tarafından web sözlüğünden (core/i18n/sections) üretildi. Elle düzenlemeyin;
// metni web'de değiştirip script'i yeniden çalıştırın.
// </auto-generated>
#nullable enable
namespace LifeQuest.Mobile.Core.Localization;

/// <summary>Uygulamanın tüm kullanıcı metinleri; her okuma o anki dili döndürür.</summary>
public sealed class Strings
{
${roots.join('\n')}
}

${interfaces.join('\n\n')}

${classes.map((c) => `public sealed class ${c.name}Strings : LocalizedStrings${implementsOf.has(c.name) ? ', ' + implementsOf.get(c.name) : ''}\n{\n${c.body}\n}`).join('\n\n')}
`;
const target = join(root, 'src/LifeQuest.Mobile.Core/Localization/Strings.g.cs');
writeFileSync(target, out);
console.log(`${roots.length} bölüm, ${classes.length} sınıf → ${target}`);
