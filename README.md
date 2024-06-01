**_UPDATED FOR 2024 !!_**

# FuehrerscheinstelleAppointmentFinder
A script for periodically checking available appointments of the driving license office in Kaiserslautern

# Prerequisites
- docker OR .Net6.0 runtime (**recommended**: use dotnet runtime) (see Hints below)

# How to run the script
1. Clone the repo
2. Open `src/appsettings.json`
3. Modify the arguments according to your needs. For sending emails, provide Gmail settings (**recommended**) (see Hints below))
~~4. To run with docker~~
	~~1. Open a terminal at `src/`~~
	~~2. Run `$ docker build -t fuehrerscheinstelle_appointment_finder:latest .`~~
	~~3. Run `$ docker run -it fuehrerscheinstelle_appointment_finder`~~
5. To run natively
	1. Open a terminal at `src/`
	2. Run `$ dotnet run`

# Hints:
- For installing dotnet runtime => Ask ChatGPT: "How to install dotnet 6 runtime on windows or mac or linux"
- For getting Gmail API key => Ask ChatGPT: "How to get Gmail API keys for sending email using Gmail API"

### Further notes
If you ever face issues or have suggestions for improvement, feel free to open an issue or create pull request.
