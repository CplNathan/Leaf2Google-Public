# Leaf2Google-Public
A service which allows you to link your Nissan Connect enabled vehicle to Google Home with a nifty web interface.

## Notice
There are issues with reliability - when the Nissan/Carwings session expires the project is not re-authenticating properly - there are also lots of things I would like to address such as code readility and general code smells. If anyone would like to contribute please feel welcome! Alternatively have a look at my SurePet2Google project where I have made a lot of the google helpers and devices into a generic package, it's generally a lot better than this project and leans more torwards the way I would like this project to work. Unfortunately I am majorly limited by time and do not have much time to fix bugs or improve code, this was merely a PoC and as such lacks lots of refinement. This project has been incredibly fun but also there are so many aspects to learn and take in, while primarilly relying on third parties to reverse engineer API's etc.

## Screenshots
![Leaf2Google Example](https://github.com/CplNathan/Leaf2Google-Public/blob/main/Leaf2Google.Blazor/Server/wwwroot/img/leaf2google.png)
![Leaf2Google Responsive](https://github.com/CplNathan/Leaf2Google-Public/blob/main/Leaf2Google.Blazor/Server/wwwroot/img/leaf2google_mobile.png)
![Leaf2Google Home App](https://user-images.githubusercontent.com/16903800/191339881-d1ffe0f2-b2f5-4774-94ed-cbe85460482b.png)

## Want to do's:
* Charging scheduler
* Climate scheduler
* Decipher the SRP setup so that remote lock/unlocking is possible
* Route planner and range visualizer
* More?

## Why?
I wanted a way to heat my car up in the morning. Nissan supports scheduling heating or cooling through the car's interface, however, this is a scheduled time. I wanted to be able to integrate this into my assistants good morning routine which is something Nissan currently does not offer. Since then I have continued to take the web portion of this project further as a bit of a challenge and to familiarise myself with some newer technologies.
