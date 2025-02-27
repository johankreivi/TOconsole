# TO Console, (Transparent Overlay).
Transparent Overlay is a Windows console application that empowers you to transform any window into a visually stunning overlay. With a few simple commands, you can adjust a window's transparency, enable click-through mode, and even keep it always on top. Whether you’re multitasking, giving a presentation, or creating a custom dashboard, this tool lets you tailor your workspace exactly how you want it.

## 1. Use Cases

- **Presentations & Demos:** Highlight key application windows by adjusting transparency for a polished, professional look.
- **Multitasking:** Keep essential windows visible as overlays while you work in other applications.
- **Custom Dashboards:** Create informative overlays that display system statuses or notifications without interfering with regular workflows.
- **Accessibility:** Adjust window visibility and interaction modes to suit individual preferences and improve usability.

## 2. Features

- **Window Enumeration:** Automatically lists all active windows with non-empty titles.
- **Transparency Control:** Set any window's transparency level from fully transparent (0) to fully opaque (255).
- **Click-Through Mode:** Choose whether the window should intercept mouse events or allow clicks to pass through.
- **Always on Top:** Keep your selected window above all others, ensuring it remains visible.
- **Persistent Settings:** Save your configurations in a JSON file, so your customizations are automatically reapplied in future sessions.
- **Robust Logging:** Comprehensive logging captures every operation, making it easy to troubleshoot and review actions.

## 3. Getting Started

### Prerequisites

- **Operating System:** Windows (leveraging Win32 APIs)
- **.NET Runtime:** .NET 6.0 or later

### Installation
## 1. Clone the Repository:
- Open a terminal and run:
```bash
    git clone https://github.com/yourusername/TransparentOverlayManager.git
```

## 2. Build the Project
   - Open the solution in Visual Studio and build the project.
   - Or, from the command line:
```bash
     dotnet build
```
## 3. Run the Application
```bash
   dotnet run
```

## 4. User Guide

## 1. Launch the Application  
   Upon running, you'll see a list of all visible windows on your system.

## 2. Select a Window  
   Enter the number corresponding to the window you want to customize.

## 3. Customize Settings  
   - **Transparency:** Input a value between 0 (fully transparent) and 255 (fully opaque).
   - **Click-Through:** Choose whether the window should allow clicks to pass through (y/n).
   - **Always on Top:** Decide if the window should remain above all others (y/n).

## 4. Apply and Save  
   The application will immediately apply your settings and save them for future sessions.

## 5. Developer Overview

The codebase is structured into clear, modular components that adhere to DRY principles, making it easy to maintain and extend.

### Architecture

- **Logger:**  
  Handles logging of information, warnings, and errors to `app.log` using a simple, static wrapper.

- **SettingsManager:**  
  Manages persistent storage of window configurations in a JSON file (`settings.json`), ensuring your preferences persist between runs.

- **WindowManager:**  
  Uses P/Invoke to interact with the Windows API. Key functions include:
  - **EnumerateWindows:** Lists all visible windows.
  - **ApplySettings:** Applies transparency, click-through, and always-on-top attributes using functions such as `SetWindowLong`, `SetLayeredWindowAttributes`, and `SetWindowPos`.

- **UIHelper:**  
  Provides utility functions for handling user input and console display, ensuring a smooth user experience.

- **Program:**  
  The main entry point that orchestrates the application flow—displaying window lists, processing user selections, and coordinating settings application.

### Key Code Highlights

- **Win32 API Integration:**  
  The application leverages several critical Windows API functions for window manipulation, including:
  - `SetWindowLong` to modify extended window styles.
  - `SetLayeredWindowAttributes` to adjust transparency.
  - `SetWindowPos` to toggle the always-on-top feature.

- **Persistent JSON Settings:**  
  Uses `System.Text.Json` for serializing and deserializing settings, allowing for easy configuration management.

- **Robust Error Handling:**  
  Extensive error checking and logging ensure that any issues during API calls are logged for later review, maintaining application stability.

## 6. Contributing

Contributions are welcome! If you have suggestions, bug fixes, or new features, please submit an issue or pull request on [GitHub](https://github.com/johankreivi/TOconsole).

## 7. License
Use for good causes! This project is free to use with no specific license restrictions. However, please credit the original author and consider sharing your improvements with the community.

---

Enjoy customizing your workspace with Transparent Overlay Manager!