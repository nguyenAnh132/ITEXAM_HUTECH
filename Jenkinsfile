pipeline {
  agent any
  options { timestamps() }   // <-- bỏ ansiColor

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
          url: 'https://github.com/nguyenAnh132/ITExamHUTECH_NCKH.git',
          branch: 'main',
          credentialsId: 'github_pat'   // đổi ID đúng với credential của bạn
        )
      }
    }

    stage('Restore & Build') {
      steps {
        sh '''
          set -e
          dotnet --info
          dotnet restore ITExam/ITExam.sln
          dotnet build   ITExam/ITExam.sln -c Release --no-restore
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

    stage('Stop service') { steps { sh 'sudo systemctl stop ${SERVICE_NAME} || true' } }

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

    stage('Start & Health') {
      steps {
        sh '''
          sudo systemctl start ${SERVICE_NAME}
          sleep 2
          curl -fsS http://127.0.0.1:5000/ >/dev/null || (echo "❌ Health check fail" && exit 1)
          echo "✅ Deployed OK"
        '''
      }
    }
  }

  post {
    always  { archiveArtifacts artifacts: 'publish/**', fingerprint: true, onlyIfSuccessful: false }
    failure { echo '❌ Pipeline failed. Mở Console Output để xem lỗi.' }
  }
}
