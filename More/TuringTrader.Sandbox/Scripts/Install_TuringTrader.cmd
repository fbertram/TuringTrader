curl -L "https://firebasestorage.googleapis.com/v0/b/turingtrader-org.appspot.com/o/downloads%2FTuringTrader_Setup-16.0.msi?alt=media&token=a48da81e-8d77-4d9e-a585-a59727d2dd81" --output C:\users\WDAGUtilityAccount\Desktop\tt.msi
cmd /c msiexec /package C:\users\WDAGUtilityAccount\Desktop\tt.msi /quiet
cmd /c "c:\Program Files\TuringTrader\Bin\TuringTrader.exe"
