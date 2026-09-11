// The design's own Tailwind configuration, taken verbatim from
// specs/design/moni_s_workspace_home/code.html. This file is the single source of the
// palette, the type scale, the spacing scale and the shape scale.
module.exports = {
  content: ["../Components/**/*.razor", "../wwwroot/**/*.html"],
  plugins: [require("@tailwindcss/container-queries"), require("@tailwindcss/forms")],
  darkMode: "class",
            theme: {
                extend: {
                    "colors": {
                        "secondary-fixed-dim": "#9ed1bd",
                        "surface-container-high": "#e7e8e9",
                        "on-tertiary-fixed": "#331200",
                        "primary-container": "#008282",
                        "inverse-primary": "#6fd7d6",
                        "surface-container-low": "#f3f4f5",
                        "tertiary-container": "#bb580d",
                        "surface-dim": "#d9dadb",
                        "on-secondary-fixed-variant": "#1d4f40",
                        "on-primary-fixed": "#002020",
                        "surface-tint": "#006a6a",
                        "on-tertiary-fixed-variant": "#763300",
                        "on-secondary": "#ffffff",
                        "on-secondary-container": "#3d6d5d",
                        "surface-variant": "#e1e3e4",
                        "on-primary-container": "#f3fffe",
                        "surface-bright": "#f8f9fa",
                        "on-secondary-fixed": "#002117",
                        "tertiary-fixed": "#ffdbc9",
                        "outline": "#6d7979",
                        "tertiary-fixed-dim": "#ffb68d",
                        "inverse-surface": "#2e3132",
                        "on-error-container": "#93000a",
                        "secondary-container": "#baeed9",
                        "on-surface": "#191c1d",
                        "background": "#f8f9fa",
                        "on-primary-fixed-variant": "#004f4f",
                        "on-tertiary-container": "#fffbff",
                        "on-primary": "#ffffff",
                        "primary-fixed": "#8cf3f3",
                        "on-surface-variant": "#3d4949",
                        "inverse-on-surface": "#f0f1f2",
                        "on-background": "#191c1d",
                        "on-error": "#ffffff",
                        "surface": "#f8f9fa",
                        "surface-container-lowest": "#ffffff",
                        "surface-container": "#edeeef",
                        "on-tertiary": "#ffffff",
                        "error": "#ba1a1a",
                        "error-container": "#ffdad6",
                        "primary-fixed-dim": "#6fd7d6",
                        "secondary-fixed": "#baeed9",
                        "tertiary": "#974400",
                        "outline-variant": "#bcc9c8",
                        "primary": "#006767",
                        "surface-container-highest": "#e1e3e4",
                        "secondary": "#376757"
                    },
                    "borderRadius": {
                        "DEFAULT": "0.25rem",
                        "lg": "0.5rem",
                        "xl": "0.75rem",
                        "full": "9999px"
                    },
                    "spacing": {
                        "stack-lg": "32px",
                        "gutter": "24px",
                        "unit": "8px",
                        "stack-sm": "8px",
                        "margin-mobile": "16px",
                        "section-gap": "80px",
                        "container-max": "1280px",
                        "margin-desktop": "40px",
                        "stack-md": "16px"
                    },
                    "fontFamily": {
                        "body-lg": ["Plus Jakarta Sans"],
                        "display-lg": ["Plus Jakarta Sans"],
                        "body-md": ["Plus Jakarta Sans"],
                        "label-sm": ["Manrope"],
                        "headline-lg-mobile": ["Plus Jakarta Sans"],
                        "headline-md": ["Plus Jakarta Sans"],
                        "headline-lg": ["Plus Jakarta Sans"],
                        "label-md": ["Manrope"]
                    },
                    "fontSize": {
                        "body-lg": ["18px", { "lineHeight": "28px", "fontWeight": "400" }],
                        "display-lg": ["48px", { "lineHeight": "56px", "letterSpacing": "-0.02em", "fontWeight": "700" }],
                        "body-md": ["16px", { "lineHeight": "24px", "fontWeight": "400" }],
                        "label-sm": ["12px", { "lineHeight": "16px", "letterSpacing": "0.05em", "fontWeight": "700" }],
                        "headline-lg-mobile": ["28px", { "lineHeight": "36px", "fontWeight": "700" }],
                        "headline-md": ["24px", { "lineHeight": "32px", "fontWeight": "600" }],
                        "headline-lg": ["32px", { "lineHeight": "40px", "letterSpacing": "-0.01em", "fontWeight": "700" }],
                        "label-md": ["14px", { "lineHeight": "20px", "letterSpacing": "0.02em", "fontWeight": "600" }]
                    }
                }
            }
};
