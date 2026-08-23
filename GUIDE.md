# Aurora MarvinS User Guide

This guide explains how to install, set up and use Aurora MarvinS with your Aurora 4X C# 2.7.1 game data.

> **Note:** This guide is currently a work in progress and may be updated as Aurora MarvinS develops.

---

## Table of Contents

- [Getting Started](#getting-started)
- [Installation](#installation)
- [Loading Your Aurora Database](#loading-your-aurora-database)
- [Charts](#charts)
  - [Chart Navigation](#chart-navigation)
  - [X-Axis](#x-axis)
  - [Zoom and Reset](#zoom-and-reset)
  - [Events](#events)
  - [Exporting Data](#exporting-data)
- [Mineral Analysis](#mineral-analysis)
  - [Mineral Selection](#mineral-selection)
  - [Compare Minerals](#compare-minerals)
  - [Trends](#trends)
  - [Linear Projection](#linear-projection)
  - [Custom Mineral Colors](#custom-mineral-colors)
- [Tech Tree](#tech-tree)
  - [Tech Tree Layout](#tech-tree-layout)
  - [Research Status](#research-status)
  - [Currently Researched Technologies](#currently-researched-technologies)
  - [Unlocked Technologies](#unlocked-technologies)
  - [Technology Dependencies](#technology-dependencies)
- [Themes](#themes)

---

# Getting Started

Aurora MarvinS is a tool for viewing and analysing data from an Aurora 4X C# 2.7.1 game.

It reads information from your Aurora database and presents it through charts, resource analysis tools and a redesigned Tech Tree.

Aurora MarvinS does **not** modify your Aurora database.

New data points are generated in Aurora's database when you save your game. Aurora 4X does **not** automatically save your progress, so new data points will not be created unless you manually save the game.

To start building up data in Aurora MarvinS, launch the application and then save your Aurora game while Aurora MarvinS is running. The new data point will then become available to Aurora MarvinS.

Aurora MarvinS cannot detect data points created by saves that were made while the application was not running. If you save your Aurora game while Aurora MarvinS is closed, those new data points will not be available when you start the application later.

---

# Installation

## Downloading Aurora MarvinS

The easiest way to get Aurora MarvinS is to download the latest release from the [GitHub Releases](https://github.com/SanekTheDev/AuroraMarvinS/releases) page.

1. Open the [Releases](https://github.com/SanekTheDev/AuroraMarvinS/releases) page.
2. Download the latest release ZIP file.
3. Extract the ZIP file to a location of your choice.
4. Run `AuroraMarvinS.exe`.

For the latest available version, see the [Releases](https://github.com/SanekTheDev/AuroraMarvinS/releases) page on GitHub.

## Building from Source

If you want to build Aurora MarvinS yourself instead of using the pre-built release:

### Requirements

- Visual Studio 2022
- .NET Framework development tools
- The Aurora MarvinS source code

### Build Steps

1. Clone or download the Aurora MarvinS source code from this repository.
2. Open `AuroraMarvin.sln` in Visual Studio 2022.
3. Allow Visual Studio to restore any required NuGet packages if prompted.
4. Select the `Release` configuration.
5. Build or rebuild the solution using **Build → Build Solution** or **Build → Rebuild Solution**.
6. The compiled application will be available in the project's `bin\Release` directory.

The source code is provided under the **GNU General Public License v3.0 (GPL-3.0)**. See [LICENSE](LICENSE) for the full license text.

---

# Loading Your Aurora Database

To use most of the features in Aurora MarvinS, you need to select your Aurora database.

The database file is located inside the main Aurora 4X game folder and is called:

```text
AuroraDB.db
```

Select this database file to load your game data into Aurora MarvinS.

Aurora MarvinS reads the information stored in this database and uses it to populate the application's charts, resource data and Tech Tree.

> **Important:** Aurora MarvinS is a visualization and analysis tool. It does not modify or delete technologies or other data in your Aurora database.

---

# Charts

Aurora MarvinS provides several charts for analysing historical Aurora data.

Available chart types include resource, population, wealth, fuel and maintenance data.

## Chart Navigation

Charts display historical data points recorded from your Aurora database.

The charts can be interacted with to examine different parts of your campaign history.

The available chart controls include:

- Selecting and displaying different data series.
- Showing or hiding individual series through the chart legend.
- Zooming into specific parts of the chart.
- Panning through the available history.
- Resetting the current zoom.
- Displaying additional trend and analysis information where supported.
- Adding events to important points in your campaign.
- Exporting chart information where supported.

## X-Axis

Aurora MarvinS supports different ways of displaying time on the X-axis.

The available options include:

- **Relative Time** — displays the progression of your recorded data relative to the available data points.
- **Game Time** — uses the game-time information recorded by Aurora.

When changing the X-axis, the chart zoom is reset so that the new axis can be displayed correctly.

## Zoom and Reset

Charts can be zoomed and navigated to inspect specific parts of your campaign history.

Use **Reset Zoom** to return the chart to its default view.

Zoom is also reset when changing the selected X-axis.

## Events

Events can be used to mark important moments in your Aurora campaign.

For example:

- Major battles
- Colonization events
- Important technological breakthroughs
- Economic changes
- Major ship construction
- Other events you want to remember

### Add Event

Use **Add Event** to add an event marker to the chart.

Events are intended to remain part of your campaign history, allowing you to build a timeline of important moments as your campaign progresses.

### Manage Events

Use **Manage Events** to manage existing event markers.

This allows you to manage events that have already been added to the chart. Currently, existing events can only be deleted.

### Clear Compare

**Clear Compare** is used to clear the current mineral comparison.

## Exporting Data

Aurora MarvinS provides export functionality for supported chart data.

The chart toolbar includes an **Export** button.

---

# Mineral Analysis

The Mineral tab provides tools for analysing mineral resources throughout your campaign.

## Mineral Display

The Mineral view allows you to select the mineral resources that you want to examine.

## Compare Minerals

The mineral comparison functionality allows multiple minerals to be viewed and analysed together.

Use **Compare Minerals** to enable mineral comparison.

This can be useful for comparing the development of different resources over the same period of your campaign.

The comparison can be combined with other analysis features such as:

- Trend visualization
- Overlays

Use **Clear Compare** to remove the current mineral comparison.

## Trends

The trend functionality can be used to visualize whether selected mineral quantities are increasing or decreasing over time.

Use **Show Trend** to display trend information for the selected minerals.

The trend indicators can help identify whether a resource is generally increasing or decreasing based on the recorded data.

## Overlays and Analysis

Aurora MarvinS provides several optional overlays that can be displayed on charts to make it easier to analyse historical data and identify trends.

### Moving Average 3

The **Moving Average 3** overlay calculates a moving average using the three most recent available data points.

It can help smooth short-term fluctuations in the data and make the overall trend easier to see.

### Moving Average 5

The **Moving Average 5** overlay calculates a moving average using the five most recent available data points.

Compared with Moving Average 3, it provides a smoother representation of the overall trend by taking more data points into account.

### Start Value Baseline

The **Start Value Baseline** overlay creates a reference line based on the starting value of the selected data.

This makes it easier to compare later values against the value at the beginning of the displayed data range and quickly see how much the resource has changed.

### Linear Projection

The **Linear Projection** overlay displays a projected direction of change based on the available historical data.

It can be used together with other analysis features, such as mineral comparison and trend visualization.

> **Important:** A linear projection is an analytical visualization and should not be interpreted as a guaranteed prediction of future Aurora game data.

The projection is based on the available recorded data and is intended as an additional tool for analysing resource trends.

## Custom Mineral Colors

Aurora MarvinS allows individual mineral resources to have customizable colors.

This can make individual resources easier to distinguish when several minerals are displayed at the same time.

The button for changing mineral colors is located at the top of the application, on the same toolbar as the theme and Help controls.

---

# Tech Tree

Aurora MarvinS includes a redesigned Tech Tree for visualizing Aurora research dependencies.

The layout is inspired by the Tech Tree layout shared in the following Reddit post:

[Tech Tree layout inspiration](https://www.reddit.com/r/aurora/comments/1vrzho8/complete_tech_tree_for_aurora_271/)

The implementation in Aurora MarvinS is an independent implementation.

## Tech Tree Layout

Technologies are organized from left to right according to their research progression.

Technologies that follow the same research progression are positioned next to each other where possible.

Technologies that unlock multiple technologies are positioned so that the relationships between their unlocked technologies are easier to follow.

The Tech Tree uses color-coded technology fields and dependency lines.

When a technology connects to another technology field, the connection indicates where the dependency originates.

Technologies that act as dependencies for multiple technology fields can use multiple colors to represent their relationships with different fields.

## Research Status

Aurora MarvinS can display research status based on the information available in the Aurora database.

Technologies can be identified as:

- **Already researched**
- **Currently being researched**
- **Not yet researched**

Research status is updated based on the research information stored in the Aurora database.

The Tech Tree can therefore be used not only as a reference for research dependencies, but also as an overview of your current research progress.

## Currently Researched Technologies

The Tech Tree can be filtered to highlight technologies that are currently being researched.

This makes it easier to identify the technology or technologies that Aurora is currently working on.

## Unlocked Technologies

The Tech Tree can also be filtered to display technologies that have already been unlocked.

This provides a simplified view of the technologies currently available to the player.

## Technology Dependencies

Technology dependency lines show which technologies unlock or require other technologies.

Aurora MarvinS also supports dependencies between different technology fields.

The dependency visualization uses colors to help indicate the origin of a dependency when technologies from different parts of the Tech Tree are connected.

The Tech Tree also displays research costs on technologies where this information is available.

### Technologies Not Displayed

Some technologies are intentionally filtered from the displayed Tech Tree.

This includes system/start technologies as well as BioEnergy and Swarm technologies that are not intended to be displayed as player research technologies in the Aurora MarvinS Tech Tree.

These filters only affect the visualization.

They **do not remove or modify the technologies in the Aurora database**.

---

# Themes

Aurora MarvinS supports both **Light** and **Dark** themes. The theme can be changed at the top of the application, next to the resource color controls.

# Important Notes

Aurora MarvinS is an unofficial continuation/modification of the original Aurora Marvin project.

It is not the official Aurora 4X application and is not affiliated with the Aurora 4X developer.

Please do not report Aurora MarvinS bugs to the official Aurora 4X developer.

---

# Credits

Aurora MarvinS is based on the original Aurora Marvin project created by **Scnaeg**.

Original project:

[Original Aurora Marvin repository](https://gitlab.com/Scnaeg/auroramarvin)

Aurora MarvinS is maintained and developed by **Sanek**.

See [CREDITS.md](CREDITS.md) for additional information.

---

# AI Assistance

ChatGPT was used to assist with parts of the development, implementation and refinement of Aurora MarvinS.

AI assistance is disclosed in the project documentation and in the application's Info section.

---

# License

Aurora MarvinS is released under the **GNU General Public License v3.0 (GPL-3.0)**.

See [LICENSE](LICENSE) for the full license text.
