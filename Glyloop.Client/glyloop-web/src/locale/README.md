# Internationalization (i18n) Files

This directory contains translation files for the Glyloop web application.

## Source Locale
- **en-US** (English - United States) - Default source locale

## Supported Locales
- **es** - Spanish (`messages.es.xlf`)

## Extracting Messages

To extract translatable messages from your source code:

```bash
ng extract-i18n --output-path src/locale
```

This will generate a `messages.xlf` file containing all i18n messages marked in templates and TypeScript files.

## Adding a New Language

1. Create a new XLIFF file: `messages.[locale].xlf`
2. Add the locale configuration to `angular.json` under `projects.glyloop-web.i18n.locales`
3. Translate the source strings to target strings
4. Build with the locale: `ng build --localize`

## Testing Translations

To serve the application with a specific locale:

```bash
ng serve --configuration=development --localize
```

## Translation Format

Translations use XLIFF 2.0 format. Each translation unit has:
- `id`: Unique identifier (e.g., `register.title`)
- `source`: Original English text
- `target`: Translated text

Example:
```xml
<unit id="register.title">
  <segment>
    <source>Create your account</source>
    <target>Crea tu cuenta</target>
  </segment>
</unit>
```

## ID Naming Convention

Use dot-notation for hierarchical organization:
- `[feature].[component].[element].[property]`
- Example: `register.form.email.error.required`

Prefix with `@@` in templates: `i18n="@@register.form.email.label"`

