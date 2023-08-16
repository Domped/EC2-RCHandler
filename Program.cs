// See https://aka.ms/new-console-template for more information


using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Newtonsoft.Json;

class RCHandler {
    private static string URL = "https://sqs.eu-central-1.amazonaws.com/803864580155/EC2Fifo.fifo";
    // private static string URL = "https://sqs.eu-central-1.amazonaws.com/816971564981/EC2Work";

    private static string PATH = "C:/Users/mbudy/Desktop/RCScripts/";
    private static readonly AmazonStepFunctionsClient _stepClient = new AmazonStepFunctionsClient();
    private static readonly AmazonS3Client _s3BucketClient = new AmazonS3Client();
    private static readonly TransferUtility _s3TransferUtility = new TransferUtility();
    
    static async Task Main(string[] args) {
        var client = new AmazonSQSClient();
        do {
            var message = await GetMessage(client);
            if (message.Messages.Count != 0) {

                if (ProcessMessage(message.Messages[0])) {
                    var deleteResponse = await DeleteMessage(client, message.Messages[0]);
                    if (deleteResponse.HttpStatusCode != HttpStatusCode.OK) {
                        await DeleteMessage(client, message.Messages[0]);
                    }
                }
            }
        } while (!Console.KeyAvailable);
    }
    
    private static async Task<ReceiveMessageResponse> GetMessage(AmazonSQSClient client) {
        return await client.ReceiveMessageAsync(new ReceiveMessageRequest {
            QueueUrl = URL,
            MaxNumberOfMessages = 1
        });
    }

    private static void ClearWorkingImageDirectory() {
        
        DirectoryInfo di = new DirectoryInfo(PATH + "Images");
        foreach (FileInfo file in di.GetFiles()) {
            file.Delete(); 
        }
        
        di = new DirectoryInfo(PATH + "Models");
        foreach (FileInfo file in di.GetFiles()) {
            file.Delete(); 
        }
        
        di = new DirectoryInfo(PATH + "Projects");
        foreach (FileInfo file in di.GetFiles()) {
            file.Delete(); 
        }
    }
    
    private static bool ProcessMessage(Message message) {
        Console.WriteLine("Processing message: " + message.Body);
        
        var deser = JsonConvert.DeserializeObject<Dictionary<string, string>>(message.Body);
        foreach (KeyValuePair<string, string> v in  deser) {
            Console.WriteLine(v.Key + " " + v.Value);
        }

        ClearWorkingImageDirectory();

        _s3TransferUtility.DownloadDirectory(
                "finishedmodels222241-dev",
                "/public/" + deser["Folder"],
                PATH + "Images"
            );

        var startInfo = new ProcessStartInfo();
        startInfo.FileName = PATH + "Scripts/HeadReconstruction.bat";
        startInfo.WorkingDirectory = PATH;
        startInfo.UseShellExecute = true;
        var rc = Process.Start(startInfo);
        rc?.WaitForExit();
        
        //TODO: ERror checking
        
        
        _s3TransferUtility.UploadDirectory(
            PATH + "Models",
            "finishedhelmetmodels/"+ deser["Folder"]
        );
        
        _stepClient.SendTaskSuccessAsync(new SendTaskSuccessRequest {
            TaskToken = deser["TaskToken"],
            Output = "{\"status\": \"success\", \"arnEndpoint\": \"" + deser["ARN"] +"\", \"user_id\": \"" 
                     + deser["UserID"] +"\", \"folder\": \"" + deser["Folder"] +"\"}"
        });
        
        Console.WriteLine("Sent success message");
        
        return true;
    }
    
    private static async Task<DeleteMessageResponse> DeleteMessage(AmazonSQSClient client, Message message) {
        return await client.DeleteMessageAsync(new DeleteMessageRequest {
            QueueUrl = URL,
            ReceiptHandle = message.ReceiptHandle
        });
    }
}