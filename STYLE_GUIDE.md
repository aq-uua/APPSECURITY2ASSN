# Farm Fresh Market - Style Guide

## Overview

This style guide documents the visual design system for Farm Fresh Market, a sustainability-focused e-commerce platform for organic produce and ethically-raised meats. The design philosophy centers on organic, earthy aesthetics that reflect our commitment to sustainable farming practices.

---

## Design Philosophy

### Core Principles
- **Organic & Natural**: Design elements should feel grown, not manufactured
- **Warm & Welcoming**: Create an inviting atmosphere that feels like a local farm market
- **Sustainable**: Visual language that communicates environmental responsibility
- **Trustworthy**: Professional yet approachable aesthetics that build customer confidence

### Design Direction
The aesthetic is **Refined Organic** - combining elegant typography with earthy colors and soft, natural shapes. Avoid harsh edges, preferring organic curves and gentle gradients that mirror the natural world.

---

## Color System

### Primary Colors

| Name | Hex | Usage |
|------|-----|-------|
| Sage Green | `#7A9E7E` | Primary brand color, buttons, accents |
| Sage Light | `#A8C6AB` | Hover states, backgrounds |
| Sage Dark | `#5A7A5E` | Active states, emphasis |

### Secondary Colors

| Name | Hex | Usage |
|------|-----|-------|
| Terracotta | `#D4A373` | CTAs, highlights, warm accents |
| Terracotta Light | `#E8C9A0` | Hover states, decorative elements |
| Terracotta Dark | `#B8834F` | Active states, links |

### Neutral Colors

| Name | Hex | Usage |
|------|-----|-------|
| Warm White | `#FDFCF8` | Page background |
| Cream | `#F5F5DC` | Card backgrounds, sections |
| Cream Light | `#FEFEF5` | Input backgrounds |
| Earth Brown | `#3D3D3D` | Primary text |
| Earth Brown Light | `#5A5A5A` | Secondary text, captions |

### Accent Colors

| Name | Hex | Usage |
|------|-----|-------|
| Leaf Green | `#6B8E6B` | Success states, positive indicators |
| Forest | `#2D4A2D` | Headings, strong emphasis |

### Color Usage Patterns

```css
/* Primary Button */
background: linear-gradient(135deg, var(--color-sage) 0%, var(--color-sage-dark) 100%);

/* Hero Section */
background: linear-gradient(135deg, var(--color-sage) 0%, var(--color-forest) 100%);

/* Card Background */
background: white;

/* Page Background */
background-color: var(--color-warm-white);
```

---

## Typography

### Font Families

#### Display Font: Playfair Display
- **Usage**: Headings (H1-H6), brand name, titles
- **Weights**: 400 (Regular), 500 (Medium), 600 (SemiBold), 700 (Bold)
- **Character**: Elegant serif with high contrast, conveys quality and tradition
- **Fallback**: Georgia, serif

#### Body Font: Quicksand
- **Usage**: Body text, UI elements, buttons, labels
- **Weights**: 300 (Light), 400 (Regular), 500 (Medium), 600 (SemiBold), 700 (Bold)
- **Character**: Friendly geometric sans-serif, approachable and modern
- **Fallback**: 'Segoe UI', sans-serif

### Type Scale

| Element | Size | Weight | Line Height | Font |
|---------|------|--------|-------------|------|
| H1 | 3rem (48px) | 600 | 1.2 | Playfair Display |
| H2 | 2.25rem (36px) | 600 | 1.3 | Playfair Display |
| H3 | 1.75rem (28px) | 600 | 1.3 | Playfair Display |
| Body | 1rem (16px) | 400 | 1.6 | Quicksand |
| Lead | 1.25rem (20px) | 400 | 1.5 | Quicksand |
| Small | 0.875rem (14px) | 400 | 1.5 | Quicksand |
| Button | 1rem (16px) | 600 | 1 | Quicksand |

### Typography Patterns

- **Headings**: Use Forest Green (`#2D4A2D`) for strong contrast
- **Body Text**: Use Earth Brown (`#3D3D3D`) for comfortable reading
- **Links**: Use Sage Dark (`#5A7A5E`), transitioning to Terracotta on hover
- **Emphasis**: Use bold weight (600-700) sparingly for impact

---

## Spacing System

### Spacing Scale

| Token | Value | Usage |
|-------|-------|-------|
| --spacing-xs | 0.5rem (8px) | Tight spacing, icon gaps |
| --spacing-sm | 1rem (16px) | Default padding, margins |
| --spacing-md | 1.5rem (24px) | Card padding, section gaps |
| --spacing-lg | 2rem (32px) | Large gaps, hero padding |
| --spacing-xl | 3rem (48px) | Section breaks |
| --spacing-xxl | 5rem (80px) | Major section padding |

### Spacing Patterns

- **Card Padding**: 1.5rem - 2rem (`--spacing-md` to `--spacing-lg`)
- **Section Padding**: 3rem - 5rem (`--spacing-xl` to `--spacing-xxl`)
- **Component Gaps**: 1rem (`--spacing-sm`)
- **Form Field Margins**: 1rem bottom (`--spacing-sm`)

---

## Border Radius

The border radius system uses organic, soft curves that feel natural and friendly.

| Token | Value | Usage |
|-------|-------|-------|
| --radius-sm | 8px | Small elements, tags |
| --radius-md | 16px | Buttons, inputs |
| --radius-lg | 24px | Cards, containers |
| --radius-xl | 32px | Large cards, modals |
| --radius-full | 50% | Circular elements, avatars |

### Radius Patterns

- **Buttons**: 16px (`--radius-md`)
- **Form Inputs**: 16px (`--radius-md`)
- **Cards**: 24px (`--radius-lg`)
- **Progress Bars**: 50% (`--radius-full`)

---

## Shadows

Shadows are soft and natural, creating depth without harshness.

| Token | Value | Usage |
|-------|-------|-------|
| --shadow-sm | `0 2px 8px rgba(61, 61, 61, 0.08)` | Subtle elevation |
| --shadow-md | `0 4px 16px rgba(61, 61, 61, 0.12)` | Cards, hover states |
| --shadow-lg | `0 8px 32px rgba(61, 61, 61, 0.16)` | Modals, dropdowns |
| --shadow-glow | `0 0 20px rgba(122, 158, 126, 0.3)` | Focus states |

### Shadow Patterns

- **Default Cards**: `--shadow-sm`
- **Cards on Hover**: `--shadow-lg` with transform
- **Buttons on Hover**: `--shadow-md` with translateY(-2px)
- **Focused Inputs**: `--shadow-glow`

---

## Components

### Buttons

#### Primary Button
```css
background: linear-gradient(135deg, var(--color-sage) 0%, var(--color-sage-dark) 100%);
color: white;
padding: 0.75rem 1.5rem;
border-radius: 16px;
font-weight: 600;
box-shadow: var(--shadow-sm);
```

**States:**
- **Hover**: Darker gradient, translateY(-2px), `--shadow-md`
- **Active**: Scale(0.98), darker background
- **Disabled**: Opacity 0.6, cursor not-allowed

#### Secondary Button
```css
background-color: var(--color-terracotta);
color: white;
padding: 0.75rem 1.5rem;
border-radius: 16px;
font-weight: 600;
```

#### Outline Button
```css
background-color: transparent;
border: 2px solid var(--color-sage);
color: var(--color-sage);
padding: 0.75rem 1.5rem;
border-radius: 16px;
font-weight: 600;
```

### Cards

#### Standard Card
```css
background: white;
border-radius: 24px;
box-shadow: var(--shadow-sm);
padding: 1.5rem;
transition: all 0.3s ease;
```

**Hover Effect:**
```css
transform: translateY(-4px);
box-shadow: var(--shadow-lg);
```

#### Feature Card
```css
text-align: center;
padding: 2rem;
background: white;
border-radius: 32px;
box-shadow: var(--shadow-sm);
```

**Content Pattern:**
- Large emoji/icon (3rem) at top
- Heading (H3) below icon
- Description text

### Form Elements

#### Text Input
```css
border: 2px solid var(--color-cream);
border-radius: 16px;
padding: 0.75rem 1rem;
background-color: var(--color-warm-white);
font-family: var(--font-body);
transition: all 0.3s ease;
```

**Focus State:**
```css
border-color: var(--color-sage);
box-shadow: 0 0 0 0.2rem rgba(122, 158, 126, 0.25);
background-color: white;
```

#### Select Dropdown
Same styling as text input with custom arrow indicator.

#### Label
```css
font-weight: 600;
color: var(--color-earth-brown);
margin-bottom: 0.5rem;
```

### Navigation

#### Navbar
```css
background: linear-gradient(135deg, var(--color-cream) 0%, var(--color-warm-white) 100%);
box-shadow: var(--shadow-sm);
padding: 1rem 0;
```

#### Brand Link
```css
font-family: var(--font-display);
font-size: 1.75rem;
font-weight: 700;
color: var(--color-forest);
```

**Icon:** 🌿 (leaf emoji) preceding text

#### Nav Links
```css
font-weight: 500;
color: var(--color-earth-brown);
padding: 0.5rem 1rem;
border-radius: 8px;
transition: all 0.3s ease;
```

**Hover:** Background `--color-sage-light`, text `--color-forest`

### Progress Bar

```css
height: 8px;
background-color: var(--color-cream);
border-radius: 50%;
overflow: hidden;
```

**Fill:**
```css
background: linear-gradient(90deg, var(--color-sage) 0%, var(--color-terracotta) 100%);
border-radius: 50%;
```

---

## Layout Patterns

### Hero Section
```css
background: linear-gradient(135deg, var(--color-sage) 0%, var(--color-forest) 100%);
color: white;
padding: 5rem 0;
position: relative;
overflow: hidden;
```

**Content:**
- Centered text alignment
- Large heading (white)
- Subheading text (white with opacity)
- Two buttons (Primary + Outline)

**Decorative Elements:**
- Floating emojis with `animate-float` class
- Subtle radial gradient overlay

### Section Spacing
```css
padding-top: 3rem;
padding-bottom: 3rem;
```

Use alternating backgrounds (white / cream) to create visual rhythm.

### Grid System
- Use Bootstrap's 12-column grid
- Gutters: 1.5rem (24px)
- Max container width: 1140px

---

## Animations

### Fade In Up
```css
@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
/* Duration: 0.6s, Easing: ease-out */
```

### Leaf Float
```css
@keyframes leafFloat {
  0%, 100% { transform: translateY(0) rotate(0deg); }
  50% { transform: translateY(-10px) rotate(5deg); }
}
/* Duration: 3s, Easing: ease-in-out, Infinite */
```

### Hover Transitions
```css
transition: all 0.3s ease;
```

### Transform Patterns
- **Cards on hover**: `translateY(-4px)`
- **Buttons on hover**: `translateY(-2px)`
- **Active states**: `scale(0.98)`

---

## Iconography

### Emoji Usage
Use emoji as decorative icons to add personality and warmth:

- 🌿 - Brand/Logo
- 🌱 - Growth/Organic
- 🚜 - Farming/Local
- ♻️ - Sustainability
- 🌾 - Agriculture
- 🥬 - Vegetables
- 🥕 - Produce
- 🥩 - Meat
- 🥚 - Eggs
- 🥖 - Bakery
- 🍯 - Honey
- 🌍 - Environment
- 💧 - Water conservation
- 📦 - Packaging

### Usage Guidelines
- Size: 3rem for feature icons, 1.5rem for inline
- Place in feature cards, hero sections
- Use opacity for decorative elements (0.2-0.3)

---

## Responsive Breakpoints

| Breakpoint | Width | Usage |
|------------|-------|-------|
| sm | 576px | Mobile landscape |
| md | 768px | Tablets |
| lg | 992px | Small desktops |
| xl | 1200px | Desktops |

### Responsive Patterns

**Typography:**
- H1: 3rem → 2rem on mobile
- H2: 2.25rem → 1.75rem on mobile

**Spacing:**
- Section padding: 5rem → 3rem on mobile
- Container padding: Consistent 1rem

**Grid:**
- 3-column grids → 1-column on mobile
- 2-column grids → 1-column on mobile

---

## Accessibility

### Color Contrast
- All text meets WCAG AA standards (4.5:1 ratio)
- Interactive elements have visible focus states
- Links have clear hover/focus indicators

### Focus Management
```css
:focus-visible {
  outline: 3px solid var(--color-sage);
  outline-offset: 2px;
}
```

### Motion
- Respect `prefers-reduced-motion` media query
- Provide static alternatives for animations

---

## File Structure

```
WebApplication3/
├── wwwroot/
│   └── css/
│       └── site.css          # Main stylesheet with all design tokens
├── Pages/
│   ├── Index.cshtml          # Homepage
│   ├── Register.cshtml       # Registration page
│   ├── Login.cshtml          # Login page
│   └── Shared/
│       └── _Layout.cshtml    # Main layout with fonts
└── STYLE_GUIDE.md            # This file
```

---

## Implementation Notes

### CSS Variables
All design tokens are defined as CSS custom properties in `site.css`:

```css
:root {
  /* Colors */
  --color-sage: #7A9E7E;
  --color-terracotta: #D4A373;
  /* ... etc */
  
  /* Typography */
  --font-display: 'Playfair Display', Georgia, serif;
  --font-body: 'Quicksand', 'Segoe UI', sans-serif;
  
  /* Spacing */
  --spacing-sm: 1rem;
  --spacing-md: 1.5rem;
  /* ... etc */
}
```

### Google Fonts
Include in `<head>` of `_Layout.cshtml`:
```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;500;600;700&family=Quicksand:wght@300;400;500;600;700&display=swap" rel="stylesheet">
```

### Bootstrap Integration
This design system is built on top of Bootstrap 5. Override Bootstrap variables minimally and rely on custom CSS for unique styling.

---

## Version History

- **v1.0** - Initial style guide creation
- Based on organic, sustainability-focused aesthetic
- Designed for Farm Fresh Market e-commerce platform

---

## Questions?

For design implementation questions or updates to this guide, consult the development team or refer to the working examples in the codebase.
