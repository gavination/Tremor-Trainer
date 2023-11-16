# Welcome to Tremor Retrainer!

Tremor Trainer is a cross-platform mobile app lovingly built using Xamarin.Forms 5 by a team of cybernetic pandas. The application seeks to provide access to frontline therapy for those suffering from [Functional Tremors](https://pubmed.ncbi.nlm.nih.gov/27719841/). At the time of implementation, there are no known methodologies for providing a means to temper the effects of functional tremors other than Tremor Trainer. Thanks for the combined research and efforts of Drs. Jordan Garris, Alberto Espay, and Amanda Lin, we were able to implement those learnings into a simple to use mobile application that requires not much more than a smartphone (Android, iOS) with an Accelerometer. 

## Application Architecture

Currently a WIP. This section will be updated soon.

## Necessary Components and Project Organization

This project collects telemetry and crash reporting data using Visual Studio App Center. To get started, you'll have [to create a free App Center account](https://visualstudio.microsoft.com/app-center/) and add your keys to the application.

## Getting Started With This Project

To get started, the following tools must be installed on your machine:

### Get the Tools

Windows:
    - [Visual Studio 2019 Community Edition or Higher](https://visualstudio.microsoft.com/vs/)
        - While installing, be sure to check the Mobile app development workload
    - An Android phone with developer mode enabled
        - __NOTE__: since this application makes use of hardware features like the Accelerometer, it is recommeneded to prioritize using a physical Android device to test. 
    - IF running on iOS:
        - A Mac must be used to compile the application for iOS. You can use a Mac as a build machine. [Find the instructions here](https://docs.microsoft.com/en-us/xamarin/ios/get-started/installation/windows/connecting-to-mac/).

MacOS:
    - [Visual Studio for Mac 2019](https://visualstudio.microsoft.com/vs/mac/)
    - An Android phone with developer mode enabled
    - an iPhone with developer mode enabled
    - a provisioning profile ready (for physical iPhone app deployment)

Create a new application in Visual Studio App Center targeting either iOS or Android as the OS, depending on the platform you wish to build.
If you intend to build on both iOS and Android, you'll have to create 2 apps. In both cases, be sure to select Xamarin as the platform.
Copy the keys provided in the "Xamarin.Forms" section of the Getting Started guide.
In the shared project of the Tremor-Trainer (the directory without a "Tests", ".Android" or a ".iOS" suffix), create an `appsettings.json` file. This file will hold all developer secrets required to authenticate to external services, like App Center.
For reference on the structure, use the `samplesettings.json` file as guide. You can copy and rename the copy to ensure the structure is maintained.
Replace the `AndroidAppCenterSecret` and the `IOSAppCenterSecret` values with the ones obtained from Visual Studio App Center.

### Install the Dependencies

All dependencies can be installed using the NuGet Package restore function in Visual Studio. After opening the solution in Visual Studio, right click the solution and select "Restore NuGet Packages".

### Running the App

Right-click the app version you wish to run (TremorTrainer.Android or TremorTrainer.iOS) and be sure the target device is the physical phone. Then, hit F5 or the Run button to build and deploy the application.
