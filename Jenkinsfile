pipeline {
    agent any 

    stages {
        stage('Build') { 
            steps { 
                MSBuild RollingFileWithCleanupAppender.sln /t:Rebuild /p:Configuration=Release
            }
        }
    }
}