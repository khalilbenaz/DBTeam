---
title: DB TEAM — Professional SQL Server IDE
description: Modular, bilingual, themeable SQL Server IDE for Windows. Connection manager, T-SQL editor, schema & data compare, table designer, profiler, data generator, HTML documenter, ER diagram.
---

<section class="hero">
  <svg class="hero-logo" viewBox="0 0 48 48" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
    <ellipse cx="18" cy="14" rx="14" ry="4" fill="#FFC107" stroke="#111" stroke-width="2"/>
    <path d="M4 14v14c0 2.2 6.3 4 14 4s14-1.8 14-4V14" fill="#FF9800" stroke="#111" stroke-width="2"/>
    <path d="M32 24c6.6 0 12 2.2 12 6v8c0 3.8-5.4 6-12 6" fill="none" stroke="#111" stroke-width="2"/>
    <path d="M32 26 L43 26 A1 1 0 0 1 44 27 L44 40 A3 3 0 0 1 41 43 L32 43 Z" fill="#1E88E5" stroke="#111" stroke-width="2"/>
  </svg>
  <h1>DB TEAM</h1>
  <p class="lead">
    A professional SQL Server IDE for Windows — modular, bilingual (EN/FR), themeable.
    <br>
    Query, compare, design, profile, generate, document — all in one installable app.
  </p>
  <div class="hero-cta">
    <a href="https://github.com/khalilbenaz/DBTeam/releases/latest" class="btn-primary">⬇ Download installer</a>
    <a href="https://github.com/khalilbenaz/DBTeam" class="btn-ghost">View on GitHub</a>
  </div>
  <div class="hero-badges">
    <img src="https://img.shields.io/badge/.NET-8-blueviolet" alt=".NET 8">
    <img src="https://img.shields.io/badge/platform-Windows-blue" alt="Windows">
    <img src="https://img.shields.io/github/v/release/khalilbenaz/DBTeam?label=release" alt="Release">
    <img src="https://img.shields.io/badge/license-MIT-green" alt="MIT License">
    <img src="https://github.com/khalilbenaz/DBTeam/actions/workflows/ci.yml/badge.svg" alt="CI">
    <img src="https://github.com/khalilbenaz/DBTeam/actions/workflows/codeql.yml/badge.svg" alt="CodeQL">
  </div>
</section>

<section id="features" class="section">
  <h2>Features</h2>
  <p class="section-sub">Twelve modules, one shell — everything you need to work with SQL Server.</p>

  <div class="grid grid-3">
    <div class="card blue">
      <div class="icon">🗄️</div>
      <h3>Connection Manager</h3>
      <p>Windows / SQL / Azure AD auth. Credentials encrypted with DPAPI. Test, save, multi-connection.</p>
    </div>
    <div class="card blue">
      <div class="icon">🌳</div>
      <h3>Object Explorer</h3>
      <p>Lazy-loaded hierarchical tree. Icons per object type. Right-click scripting + disconnect.</p>
    </div>
    <div class="card green">
      <div class="icon">▶️</div>
      <h3>Query Editor</h3>
      <p>AvalonEdit · T-SQL syntax · autocomplete (keywords + tables + views + procs + functions + columns) · format on <kbd>Ctrl+K</kbd> · execute on <kbd>F5</kbd>.</p>
    </div>
    <div class="card orange">
      <div class="icon">⚖️</div>
      <h3>Schema Compare</h3>
      <p>Diff tables, views, procedures, functions across databases. Column-level <code>ALTER</code> generation wrapped in a transaction.</p>
    </div>
    <div class="card orange">
      <div class="icon">📋</div>
      <h3>Data Compare</h3>
      <p>Row-level diff by primary key. Produces <code>INSERT / UPDATE / DELETE</code> merge script.</p>
    </div>
    <div class="card purple">
      <div class="icon">🏗️</div>
      <h3>Table Designer</h3>
      <p>Visual editor for columns, types, nullability, identity, PK, defaults. Create or load existing tables.</p>
    </div>
    <div class="card blue">
      <div class="icon">📈</div>
      <h3>Query Profiler</h3>
      <p>Estimated + actual plan. Hierarchical plan tree with icons per physical operator and per-node stats.</p>
    </div>
    <div class="card yellow">
      <div class="icon">🎲</div>
      <h3>Data Generator</h3>
      <p>Bogus-powered fake data. Column-name heuristics: email, phone, name, city, company, etc. Preview → insert.</p>
    </div>
    <div class="card purple">
      <div class="icon">📄</div>
      <h3>Documenter</h3>
      <p>One-click HTML documentation of a database: tables, columns, indexes, FKs, views, procedures, functions.</p>
    </div>
    <div class="card blue">
      <div class="icon">🗺️</div>
      <h3>ER Diagram</h3>
      <p>Interactive canvas. Drag tables to rearrange, <kbd>Ctrl</kbd>+wheel to zoom, export as PNG.</p>
    </div>
    <div class="card green">
      <div class="icon">🕑</div>
      <h3>Query History & Favorites</h3>
      <p>Every run recorded. Filter, star favorites, double-click to reopen in a new tab.</p>
    </div>
    <div class="card orange">
      <div class="icon">📤</div>
      <h3>Export Results</h3>
      <p>CSV (RFC 4180, UTF-8 BOM) · Excel (header + filter + frozen row) · JSON. One click from the editor toolbar.</p>
    </div>
  </div>
</section>

<section class="section">
  <h2>Keyboard shortcuts</h2>
  <p class="section-sub">The essentials.</p>
  <div class="grid grid-2" style="max-width: 720px; margin: 0 auto;">
    <div class="card"><strong><kbd>F5</kbd></strong> — Execute query</div>
    <div class="card"><strong><kbd>Ctrl</kbd> + <kbd>K</kbd></strong> — Format SQL</div>
    <div class="card"><strong><kbd>Ctrl</kbd> + <kbd>Space</kbd></strong> — Autocomplete</div>
    <div class="card"><strong><kbd>Ctrl</kbd> + <kbd>N</kbd></strong> — New connection</div>
    <div class="card"><strong><kbd>Ctrl</kbd> + <kbd>Q</kbd></strong> — New query</div>
    <div class="card"><strong><kbd>Ctrl</kbd> + wheel</strong> — Zoom diagram</div>
  </div>
</section>

<section class="section">
  <h2>Module status</h2>
  <p class="section-sub">Everything shippable is shipped. Advanced items have technical design ready to pick up.</p>
  <div class="status-grid">
    <div class="label">Connection Manager</div> <div><span class="pill done">Shipped</span></div>
    <div class="label">Object Explorer</div>    <div><span class="pill done">Shipped</span></div>
    <div class="label">Query Editor + autocomplete + format</div> <div><span class="pill done">Shipped</span></div>
    <div class="label">Results export (CSV/Excel/JSON)</div> <div><span class="pill done">Shipped</span></div>
    <div class="label">Schema Compare + column-level ALTER</div> <div><span class="pill done">Shipped</span></div>
    <div class="label">Data Compare + merge script</div> <div><span class="pill done">Shipped</span></div>
    <div class="label">Table Designer (create + edit)</div> <div><span class="pill done">Shipped</span></div>
    <div class="label">Query Profiler (plan tree + stats)</div> <div><span class="pill done">Shipped</span></div>
    <div class="label">Data Generator (Bogus)</div>    <div><span class="pill done">Shipped</span></div>
    <div class="label">Documenter (HTML)</div>          <div><span class="pill done">Shipped</span></div>
    <div class="label">ER Diagram (drag, zoom, PNG)</div> <div><span class="pill done">Shipped</span></div>
    <div class="label">Query History + favorites</div>  <div><span class="pill done">Shipped</span></div>
    <div class="label">Session restore</div>            <div><span class="pill done">Shipped</span></div>
    <div class="label">Accessibility (AutomationProperties + focus)</div> <div><span class="pill done">Shipped</span></div>
    <div class="label">Theme (Light / Dark / System)</div> <div><span class="pill done">Shipped</span></div>
    <div class="label">Localization (EN / FR)</div>      <div><span class="pill done">Shipped</span></div>
    <div class="label">Installer (.exe, portable zip)</div> <div><span class="pill done">Shipped</span></div>
    <div class="label">Code signing</div>                <div><span class="pill soon">Planned</span></div>
    <div class="label">MSIX + winget</div>               <div><span class="pill soon">Planned</span></div>
    <div class="label">Auto-update (Velopack)</div>      <div><span class="pill soon">Planned</span></div>
    <div class="label">IntelliSense CTE/alias/signatures</div> <div><span class="pill soon">Planned</span></div>
    <div class="label">T-SQL Debugger (step-through)</div> <div><span class="pill soon">v2</span></div>
    <div class="label">AI assistant (BYO-key)</div>      <div><span class="pill soon">v2</span></div>
  </div>
</section>

<section class="section">
  <h2>Built with</h2>
  <p class="section-sub">Modern stack, open-source libraries, clean MVVM architecture.</p>
  <div class="stack">
    <span>.NET 8</span>
    <span>WPF</span>
    <span>AvalonDock</span>
    <span>AvalonEdit</span>
    <span>ModernWpfUI</span>
    <span>Material Design Icons</span>
    <span>CommunityToolkit.Mvvm</span>
    <span>Microsoft.Data.SqlClient</span>
    <span>ScriptDom</span>
    <span>SMO</span>
    <span>Bogus</span>
    <span>Dapper</span>
    <span>EPPlus</span>
    <span>Serilog</span>
    <span>xUnit</span>
    <span>Inno Setup</span>
    <span>GitHub Actions</span>
    <span>Dependabot</span>
    <span>CodeQL</span>
  </div>
</section>

<section class="section">
  <h2>Documentation</h2>
  <div class="grid grid-3">
    <a href="{{ '/INSTALL.html' | relative_url }}" class="card blue" style="text-decoration:none;color:inherit">
      <div class="icon">📥</div><h3>Install guide</h3>
      <p>Download, first run, uninstall.</p>
    </a>
    <a href="{{ '/USER-GUIDE.html' | relative_url }}" class="card green" style="text-decoration:none;color:inherit">
      <div class="icon">📖</div><h3>User guide</h3>
      <p>Every module explained, screen by screen.</p>
    </a>
    <a href="{{ '/ARCHITECTURE.html' | relative_url }}" class="card purple" style="text-decoration:none;color:inherit">
      <div class="icon">🏛️</div><h3>Architecture</h3>
      <p>Layers, DI, event bus, data flows.</p>
    </a>
    <a href="{{ '/MODULES.html' | relative_url }}" class="card orange" style="text-decoration:none;color:inherit">
      <div class="icon">🧩</div><h3>Modules reference</h3>
      <p>Per-module implementation notes.</p>
    </a>
    <a href="{{ '/bmad/PRD.html' | relative_url }}" class="card yellow" style="text-decoration:none;color:inherit">
      <div class="icon">🎯</div><h3>Product requirements</h3>
      <p>Vision, goals, acceptance criteria.</p>
    </a>
    <a href="{{ '/bmad/EPICS.html' | relative_url }}" class="card blue" style="text-decoration:none;color:inherit">
      <div class="icon">🗺️</div><h3>Roadmap</h3>
      <p>13 epics, detailed stories.</p>
    </a>
  </div>
</section>

<section class="section" style="text-align:center;">
  <h2>Get started</h2>
  <p class="section-sub">Self-contained Windows x64 binary. No .NET install. Per-user install, clean uninstall.</p>
  <div class="hero-cta">
    <a href="https://github.com/khalilbenaz/DBTeam/releases/latest" class="btn-primary">⬇ Latest release</a>
    <a href="https://github.com/khalilbenaz/DBTeam" class="btn-ghost">⭐ Star on GitHub</a>
    <a href="https://github.com/khalilbenaz/DBTeam/issues/new" class="btn-ghost">🐛 Report an issue</a>
  </div>
</section>
