# Veeam task

To run the sync program, please follow these steps:
1. Open the Command Prompt and navigate to the directory where syncTask.cs is located.
2. In the Command Prompt, execute the following command:
   ```bash
    csc syncTask.cs
3. Then, execute the following command:
   ```bash
    syncTask.exe sourceFolderPath replicaFolderPath logFilePath synchronizationInterval
   ```
    - Note:
      - sourceFolderPath is the path of the source folder;
      - replicaFolderPath is the path of the replica folder (if it doesn't exist, it will be created);
      - logFilePath is the path for the log file, should be a text file (if it doesn't exist, it will be created);
      - synchronizationInterval must be in the format hh:mm:ss.
  - **Example:**
    ```bash
    syncTask.exe source replica log.txt 01:00:00
