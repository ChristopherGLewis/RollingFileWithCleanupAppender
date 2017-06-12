pipeline {
    agent any 

    stages {
        stage('Build') { 
            steps { 
                bat "\"${tool 'MSBuild'}\" RollingFileWithCleanupAppender.sln /p:Configuration=Release 
 
            }
        }
    }
}