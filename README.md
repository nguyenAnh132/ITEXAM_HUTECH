# ITExam - Online Examination System
An online examination management system that helps lecturers create exams, manage question banks, and monitor students during online tests.

## Introduction
ITExam is an online examination system designed to support universities and training institutions in creating and managing exams efficiently.

The system allows lecturers to build structured exams based on Course Learning Outcomes (CLO), manage question banks, and conduct online examinations with monitoring features to reduce cheating.

The platform also integrates AI to assist in generating questions and suggesting grading for essay answers.

## Features
### Lecturer Features

- Manage question banks
- Create exams based on CLO matrix
- Manage classes and exam sessions
- Monitor students in real-time
- AI-assisted essay grading

### Student Features

- Join classes using class code
- Take multiple-choice exams
- Take essay exams
- View exam history and scores

## Technologies Used
### Backend
- ASP.NET Core MVC

### Frontend
- HTML5
- CSS3
- Bootstrap
- JavaScript
- Razor

### Database
- SQL Server
- Redis

### AI Integration
- LLM API for question generation and essay grading

## System Architecture
The system is built using the MVC (Model - View - Controller) architecture.

This architecture separates the application into three main components:

- Model: Handles data and business logic
- View: Displays the user interface
- Controller: Handles user requests and application flow

This structure improves maintainability, scalability, and code organization.

## Installation
### Clone the repository

git clone https://github.com/yourusername/itexam.git

### Open project

Open the solution using Visual Studio 2022.

### Configure Database

Update the connection string in:

appsettings.json

### Run migration

Update-Database

### Run the project

Press F5 in Visual Studio.

## Team Members
- Nguyễn Văn Tú Anh
- Đỗ Minh Phương
- Nguyễn Tấn Phát
- Trần Bá Tòng
- Trần Hải Đăng
