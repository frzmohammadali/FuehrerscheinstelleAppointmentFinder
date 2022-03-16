# FuehrerscheinstelleAppointmentFinder
A script for periodically checking available appointments of the driving license office in Kaiserslautern

# Prerequisites
- docker OR .Net6.0 runtime

# How to run the script
1. Clone the repo
2. Open `src/appsettings.json`
3. Modify the arguments according to your needs. You will need a free [SendGrid](https://signup.sendgrid.com/) account with a valid api key for sending emails.
4. To run with docker
	1. Open a terminal at `src/`
	2. Run `$ docker build -t fuehrerscheinstelle_appointment_finder:latest .`
	3. Run `$ docker run -it fuehrerscheinstelle_appointment_finder`
5. To run natively
	1. Open a terminal at `src/`
	2. Run `$ dotnet run`

### Further notes
If ou ever face issues or have suggestions for improvement, feel free to open an issue or create pull request.
