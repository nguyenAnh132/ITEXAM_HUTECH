pipeline {
  agent any
  options { timestamps() }

  environment {
    DOTNET_CLI_TELEMETRY_OPTOUT = "1"
    DOTNET_NOLOGO = "1"
    DEPLOY_DIR  = "/var/www/itexam"
    SERVICE_NAME = "itexam"
    DLL_NAME = "ITExam.dll"
  }

  stages {
    stage('Checkout') {
      steps {
        git(
          url: 'https://github.com/nguyenAnh132/ITExam_HUTECH.git',
          branch: 'main',
          credentialsId: 'github_pat'
        )
      }
    }

    stage('Restore & Build') {
      steps {
        sh '''
          set -e
          dotnet --info
          dotnet restore ITExam/ITExam.sln
          dotnet build ITExam/ITExam.sln -c Release --no-restore
        '''
      }
    }

    stage('Publish') {
      steps {
        sh '''
          set -e
          rm -rf publish
          dotnet publish ITExam/WebApplication1/ITExam.csproj -c Release -o publish --no-restore
          test -f "publish/${DLL_NAME}" || (echo "❌ Không thấy ${DLL_NAME}" && exit 1)
        '''
      }
    }

    stage('Deploy') {
      steps {
        sh '''
          set -e
          sudo mkdir -p ${DEPLOY_DIR}
          sudo rsync -a --delete publish/ ${DEPLOY_DIR}/
          sudo chown -R www-data:www-data ${DEPLOY_DIR}
          sudo chmod -R u=rwX,g=rX,o=rX ${DEPLOY_DIR}
        '''
      }
    }

    stage('Restart service & Health check') {
      steps {
        sh '''
          # restart thay vì stop/start
          sudo systemctl restart ${SERVICE_NAME}

          echo "⏳ Waiting for app..."
          for i in {1..30}; do
            if curl -fsS http://127.0.0.1:5000/ >/dev/null; then
              echo "✅ App is running!"
              exit 0
            fi
            sleep 2
          done

          echo "❌ App did not start in time"
          exit 1
        '''
      }
    }

    stage('Wait for DB') {
      steps {
        sh '''
          for i in {1..30}; do
            /opt/mssql-tools18/bin/sqlcmd -S 127.0.0.1,1433 -U sa -P 'YourStrong!Passw0rd' -Q "SELECT 1" -C && exit 0
            echo "⏳ Waiting for SQL Server..."
            sleep 2
          done
          echo "❌ SQL Server not ready"
          exit 1
        '''
      }
    }
  }

  post {
    always  { archiveArtifacts artifacts: 'publish/**', fingerprint: true, onlyIfSuccessful: false }
    failure { echo '❌ Pipeline failed. Mở Console Output để xem lỗi.' }
  }
}
