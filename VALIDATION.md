# Portfolio validation

- `net6.0` build with SDK 6.0.428: 0 errors, 0 warnings. Build invoked from `/tmp` with the project path and `-p:UseAppHost=false`, preserving the user's `global.json` pin to SDK 10.0.203, which is not installed in this validation machine.
- `python3 tests/smoke.py http://127.0.0.1:5004`: 66 checks passed, including pages, local image assets, validation, CSRF, persistence and 404s.
- `tests/browser-animations.cjs`: AOS, GSAP, navigation, progress bar, mobile menu, theme, reduced motion and JavaScript-disabled behavior passed.
- `tests/browser-typography.cjs`: original heading/body font sizes, text preservation during animation and all five locally served editorial images passed.
- `tests/browser-layout.cjs`: no horizontal overflow at 390, 768, 1024 and 1440px; all editorial images loaded with real image responses, not mocked assets.

## Compact-layout measurements

| Viewport | Previous page height | Current page height | Previous portrait width | Current portrait width |
| --- | --- | --- | --- | --- |
| 1440px | 13365px | 7505px | 572.8px | 725.3px |
| 390px | 16640px | 12185px | 318px | 342px |

Measurements used Chromium at 900px viewport height with reduced motion enabled and all images loaded. Actual font rendering may differ across devices. Content was retained while grouping related sections into columns and removing forced heading line breaks.

The stock photographs illustrate software work, devices and office operations; they are not photos of the user's actual projects. Sources and retained license notices are documented in `docs/IMAGE-SOURCES.md`. All previously failing external Unsplash image URLs have been replaced with local assets.
