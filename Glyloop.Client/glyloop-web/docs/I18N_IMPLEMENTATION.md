# Angular i18n Implementation Guide

## Overview

This project uses Angular's built-in internationalization (i18n) system with `@angular/localize` to support multiple languages. All user-facing text is externalized and ready for translation.

## Current Configuration

### Supported Locales

- **en-US** (English - United States) - Default source locale
- **es** (Spanish) - Translation file: `src/locale/messages.es.xlf`

### Configuration Files

#### angular.json
```json
{
  "i18n": {
    "sourceLocale": "en-US",
    "locales": {
      "es": { "translation": "src/locale/messages.es.xlf" }
    }
  },
  "architect": {
    "build": {
      "options": {
        "localize": false,
        "polyfills": ["@angular/localize/init"]
      }
    }
  }
}
```

**Important:** For development, use `localize: false` to enable runtime translation.

#### main.ts
```typescript
// Import @angular/localize/init at the top to ensure $localize is available
import '@angular/localize/init';

import { bootstrapApplication } from '@angular/platform-browser';
// ... rest of imports
```

**Note:** The direct import in `main.ts` is required for runtime i18n. The build warning about this import is expected and safe to ignore.

#### tsconfig.app.json
```json
{
  "compilerOptions": {
    "types": ["@angular/localize"]
  }
}
```

## Usage Patterns

### 1. Static Text in Templates

Use the `i18n` attribute with a unique ID:

```html
<h1 i18n="@@register.title">Create your account</h1>
<p i18n="@@register.subtitle">Join Glyloop to start tracking your glucose levels</p>
```

**ID Convention:** `@@[feature].[component].[element].[property]`

### 2. Element Attributes

Use `i18n-[attribute]` for translating attributes:

```html
<input 
  i18n-placeholder="@@register.form.email.placeholder"
  placeholder="you@example.com" />

<button 
  [attr.aria-label]="getPasswordVisibilityLabel()"
  i18n-aria-label="@@register.form.password.toggleLabel">
</button>
```

### 3. Inline Text Segments

Use `<ng-container>` for inline translatable segments:

```html
<p>
  <ng-container i18n="@@register.alreadyHaveAccount">Already have an account?</ng-container>
  <a href="/login" i18n="@@register.signInLink">Sign in</a>
</p>
```

### 4. Dynamic Strings in TypeScript

Use `$localize` for runtime strings:

```typescript
getEmailErrorMessage(): string {
  if (this.emailControl.hasError('required')) {
    return $localize`:@@register.form.email.error.required:Email is required`;
  }
  if (this.emailControl.hasError('email')) {
    return $localize`:@@register.form.email.error.invalid:Please enter a valid email address`;
  }
  return '';
}
```

### 5. Interpolation with Variables

Use named placeholders for interpolated values:

```typescript
const minLength = 12;
return $localize`:@@register.form.password.error.minlength:Password must be at least ${minLength}:minLength: characters`;
```

## Workflow

### 1. Extract Translatable Messages

Run the extraction command to generate/update the base translation file:

```bash
ng extract-i18n --output-path src/locale
```

This creates `src/locale/messages.xlf` with all marked strings.

### 2. Create/Update Translations

For each supported locale, create or update the corresponding `.xlf` file:

**messages.es.xlf:**
```xml
<?xml version="1.0" encoding="UTF-8" ?>
<xliff version="2.0" xmlns="urn:oasis:names:tc:xliff:document:2.0" srcLang="en-US" trgLang="es">
  <file id="ngi18n" original="ng.template">
    <unit id="register.title">
      <segment>
        <source>Create your account</source>
        <target>Crea tu cuenta</target>
      </segment>
    </unit>
  </file>
</xliff>
```

### 3. Build with Localization

Build all locales:
```bash
npm run build
```

Build specific locale for development:
```bash
ng build --localize=false --configuration=development
```

### 4. Serve with Locale

To test a specific locale during development, you need to build first then serve the built files, as `ng serve` doesn't support runtime locale switching.

## ID Naming Convention

Follow this hierarchical structure:

```
@@[feature].[component].[element].[property]
```

### Examples:

- `@@register.title` - Page title
- `@@register.form.email.label` - Form field label
- `@@register.form.email.error.required` - Validation error
- `@@register.form.password.placeholder` - Input placeholder
- `@@register.success.message` - Success message

### Benefits:

1. **Hierarchical Organization** - Easy to find related translations
2. **Collision Avoidance** - Unique IDs prevent conflicts
3. **Maintainability** - Clear structure for large projects
4. **Context Clarity** - Self-documenting IDs

## Best Practices

### 1. Always Use Unique IDs

✅ **Good:**
```html
<h1 i18n="@@register.title">Create your account</h1>
```

❌ **Bad:**
```html
<h1 i18n>Create your account</h1>
```

### 2. Keep IDs Stable

Don't change IDs when refactoring - existing translations depend on them.

### 3. Provide Context

For ambiguous terms, add meaning or description:

```html
<button i18n="@@common.button.save|Save button in form">Save</button>
```

### 4. Extract Repeated Text

Create shared translation keys for commonly used terms:

```
@@common.button.save
@@common.button.cancel
@@common.button.submit
@@common.error.required
@@common.error.invalid
```

### 5. Group Related Translations

Organize translations by feature, then by component:

```
register.*
  register.title
  register.subtitle
  register.form.*
    register.form.email.*
    register.form.password.*
```

### 6. Handle Plurals

Use ICU MessageFormat for plurals:

```typescript
$localize`:@@items.count:{count, plural, =0 {No items} =1 {1 item} other {${count} items}}`
```

### 7. Test All Locales

Before deployment, build and test each locale:

```bash
# Build all locales
npm run build

# Check output in dist/glyloop-web/[locale]/
```

## Adding a New Language

### Step 1: Add Locale Configuration

Update `angular.json`:

```json
{
  "i18n": {
    "locales": {
      "de": {
        "translation": "src/locale/messages.de.xlf"
      }
    }
  }
}
```

### Step 2: Create Translation File

Create `src/locale/messages.de.xlf`:

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<xliff version="2.0" xmlns="urn:oasis:names:tc:xliff:document:2.0" srcLang="en-US" trgLang="de">
  <file id="ngi18n" original="ng.template">
    <!-- Add translations here -->
  </file>
</xliff>
```

### Step 3: Extract and Translate

1. Extract messages: `ng extract-i18n --output-path src/locale`
2. Copy units from `messages.xlf` to `messages.de.xlf`
3. Translate all `<target>` elements
4. Build: `npm run build`

## Troubleshooting

### Build Error: "Cannot find name '$localize'"

**Solution:** Ensure `tsconfig.app.json` includes:
```json
{
  "compilerOptions": {
    "types": ["@angular/localize"]
  }
}
```

### Runtime Error: "$localize is not defined"

**Solution:** Import `@angular/localize/init` at the top of `src/main.ts`:
```typescript
import '@angular/localize/init';
```

This ensures the `$localize` function is available before any components load.

### Schema Validation Error: "must NOT have additional properties(polyfills)"

**Solution:** The `serve` builder doesn't support the `polyfills` option. Remove it from the `serve` section in `angular.json`. Polyfills should only be in the `build` section, and the direct import in `main.ts` handles runtime loading.

### Warning: "No translation found for..."

**Expected:** Translation warnings appear when strings are marked for translation but not yet translated in locale files. This is normal during development.

**To Fix:** Add the missing translation unit to the corresponding `.xlf` file.

### Runtime Error: "Missing translation for message"

**Solution:** Ensure all IDs in the code match IDs in translation files, and all locales have complete translations.

## IDE Integration

### VS Code

Install the extension: **XLIFF Sync**

Features:
- Syntax highlighting for `.xlf` files
- Translation unit validation
- Auto-completion for translation IDs

### WebStorm/IntelliJ

Built-in support for Angular i18n:
- Right-click on `i18n` attribute → "Show in translation file"
- Refactor → Rename to update IDs everywhere

## Migration from Hardcoded Strings

When converting existing hardcoded strings:

1. Identify all user-facing text
2. Add `i18n` attributes with unique IDs
3. Replace TypeScript strings with `$localize`
4. Extract messages: `ng extract-i18n`
5. Verify build succeeds
6. Test in default locale

## Resources

- [Angular i18n Guide](https://angular.dev/guide/i18n)
- [XLIFF 2.0 Specification](http://docs.oasis-open.org/xliff/xliff-core/v2.0/xliff-core-v2.0.html)
- [ICU MessageFormat](https://unicode-org.github.io/icu/userguide/format_parse/messages/)

## Current Implementation Status

### Completed:
- ✅ Installed `@angular/localize` package
- ✅ Configured i18n in `angular.json`
- ✅ Set up TypeScript types
- ✅ Converted Register view to use i18n
- ✅ Created sample translations (Spanish)
- ✅ Added polyfills configuration
- ✅ Updated frontend coding standards

### Remaining:
- ⏳ Complete all translations for Spanish
- ⏳ Add additional locales as needed
- ⏳ Convert other views (Login, Dashboard, etc.)
- ⏳ Set up CI/CD pipeline for translation extraction
- ⏳ Integrate with professional translation service (optional)

