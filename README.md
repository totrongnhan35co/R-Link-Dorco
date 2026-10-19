RLINK - BARCODE VERIFICATION SYSTEM
- Version: 1.0.7.0 Build 241014
- Language support: Vietnamese, English
- Printer support: RYNAN Printer with POD transfer (R10,R20,R60)
- Camera support: Cognex Dataman, Cognex In-Sight Vision with Multisync-Read and Basic Read
- Serial protocol devices connection: Barcode reader
- Controller support: TCP/IP transfer (PLC S7-1200)
- Operating mode:
  - Standalone[Can Read, Static Text, Database] no need for database unless using mode Standalone-database 
  - Rynan Series [After Prodution, On Production, Verify and Print] with database is requirement
  - Reprint
  - Recheck
  - Export logs

  System requirements:
  - Operating System: Windows 10 Professional (64-bit), version 20H2 or later; Windows 11 (64-bit), version 23H2 is recommended
  - Screen resolution: Recommended 1920x1080
  - Tested CPU: Intel® i5 3th, Intel® 12th Gen or newer, or AMD Ryzen™ 4000 Series or newer
  - RAM: 8GB or more
  - Local Storage: SSD with 1GB of available disk space
  - Internet: Not required
  - Additional system requirements: Microsoft .NET 4.8 or later (included)
  - Recommended network configuration: Cat5e/Cat6e, Gigabit Ethernet (1 Gbps) or higher, PCIe x4, RJ45/Optical Connector
- Account : admin / 123456

BUILD
- .NET Framework 4.8
- Winform on Visual Studio 2022 CE
- Inno Settup Installer

New Update:
- Update debug account: demo/123456
- Update stop process while missed printed page
- Remove 1ms delay received data printer
  R_Link_1.0.7.4
- Updated Position Data Through TCP/IP for Camera In-Sight.
- Compared both Position and Data.
- Added Editing Command and Index after Command.
