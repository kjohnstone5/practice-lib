pipeline {
    agent any
    environment {
        PROJECT_NAME="Sample-Lib"
        PROJECT_DLL="${env.PROJECT_NAME}.dll"
        PROJECT_ROOT="${env.PROJECT_NAME}\\${env.PROJECT_NAME}"
        CLEAN_BUILD_PATH="${env.PROJECT_ROOT}.sln"
        NUGET_RESTORE_PATH = "${env.WORKSPACE}\\${env.PROJECT_ROOT}.sln"
        RELEASE_BIN_PATH = "${env.WORKSPACE}\\${env.PROJECT_ROOT}\\bin\\Release"
        CORE_BIN_PATH = "C:\\Program Files (x86)\\Jenkins\\workspace\\_bin"
    }
    stages {
        // stage("Documentation") {
        //     steps {
        //         echo "PROJECT_NAME: ${env.PROJECT_NAME}"
        //         echo "PROJECT_ROOT: ${PROJECT_ROOT}"
        //         echo "CLEAN_BUILD_PATH: ${env.CLEAN_BUILD_PATH}"
        //         echo "NUGET_RESTORE_PATH: ${env.NUGET_RESTORE_PATH}"
        //         echo "RELEASE_BIN_PATH: ${env.RELEASE_BIN_PATH}"
        //         echo "CORE_BIN_PATH: ${env.CORE_BIN_PATH}"
        //         echo "BUILD_ID: ${env.BUILD_ID}"
        //         echo "BUILD_NUMBER: ${env.BUILD_NUMBER}"
        //         echo "BUILD_TAG: ${env.BUILD_TAG}"
        //         echo "BUILD_URL: ${env.BUILD_URL}"
        //         echo "EXECUTOR_NUMBER: ${env.EXECUTOR_NUMBER}"
        //         echo "JAVA_HOME: ${env.JAVA_HOME}"
        //         echo "JENKINS_URL: ${env.JENKINS_URL}"
        //         echo "JOB_NAME: ${env.JOB_NAME}"
        //         echo "NODE_NAME: ${env.NODE_NAME}"
        //         echo "WORKSPACE: ${env.WORKSPACE}"
        //         echo "PROJECT_NAME: ${env.PROJECT_NAME}"
        //         echo "SOLUTION_PATH: ${env.SOLUTION_PATH}"
        //     }
        // }
        stage("Checkout") {
            steps {
                retry(3) {
                    echo "================Checking out================"
                    checkout([
                        $class: "GitSCM",
                        branches: [
                            [name: "*/master"]
                        ],
                        doGenerateSubmoduleConfigurations: false,
                        extensions: [],
                        submoduleCfg: [],
                        userRemoteConfigs: [
                            [credentialsId: "32bdc15c-85df-4fea-90e5-d565c5669269",
                            url: "git@github.com:Omnitracs/sylectus-sample-dotnet-lib.git"]
                        ]
                    ])
                }
            }
        }
        stage ("Clean") {
            steps {
                retry(3) {
                    // bat "\"${tool "jenkins_msbuild_15"}\" /p:Configuration=Debug /t:Clean \"${env.CLEAN_BUILD_PATH}\""
                    bat "\"${tool "jenkins_msbuild_15"}\" /p:Configuration=Release /t:Clean \"${env.CLEAN_BUILD_PATH}\""
                }
            }
        }
        stage ("Nuget Restore") {
            steps {
                retry(3) {
                    bat label: "", script: 'nuget restore "%NUGET_RESTORE_PATH%"' //windows
                }
            }
        }
        stage ("Build") {
            steps {
                retry(3) {
                    // bat "\"${tool "jenkins_msbuild_15"}\" /p:Configuration=Debug /p:Platform=\"Any CPU\" \"%CLEAN_BUILD_PATH%\""
                    bat "\"${tool "jenkins_msbuild_15"}\" /p:Configuration=Release /p:Platform=\"Any CPU\" \"%CLEAN_BUILD_PATH%\""
                }
            }
        }
        stage ("Produce Artifact") {
            when {
                branch 'master'
            }
            steps {
                dir("${env.RELEASE_BIN_PATH}") {
                    fileOperations([fileCopyOperation(excludes: '', flattenFiles: true, includes: "${env.PROJECT_DLL}", targetLocation: "${env.CORE_BIN_PATH}")])
                }
            }
        }
    }
    post {
        cleanup {            
            dir("${env.WORKSPACE}@tmp") {
                deleteDir()
            }
        }
        success {
            // Requires email notification configuration
            // mail to: 'team@email.com',
            //  subject: "Successful Pipeline: ${currentBuild.fullDisplayName}",
            //  body: "${env.BUILD_URL} is good to go!"
        }
        failure {
            // Requires email notification configuration
            // mail to: 'team@email.com',
            //  subject: "Failed Pipeline: ${currentBuild.fullDisplayName}",
            //  body: "Something is wrong with ${env.BUILD_URL}"
        }
    }
}