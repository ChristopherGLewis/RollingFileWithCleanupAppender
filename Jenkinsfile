pipeline {
    agent any 

    stages {
        stage('Build') { 
            steps { 
                bat '"C:\\\Program Files (x86)\\NuGet\\nuget.exe" restore '
				bat 'C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\MSBuild.exe RollingFileWithCleanupAppender.sln /t:rebuild /p:Configuration=Release'
 
            }
        }
    }
}