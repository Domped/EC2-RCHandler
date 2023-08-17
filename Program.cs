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
        // if (IsModelInDirectory()) {
        //     _s3TransferUtility.UploadDirectory(
        //         PATH + "Models",
        //         "finishedhelmetmodels/"+ "dva-08.17.2023.07.16-457c3fd8-1b53-44f4-b44a-fa75981d2be2"
        //     );
        //
        //     _stepClient.SendTaskSuccessAsync(new SendTaskSuccessRequest {
        //         TaskToken ="AQCMAAAAKgAAAAMAAAAAAAAAAfZ0dqbz64dbY/CbLHn71Ej+NXLmT7PDr1egBngurwF/iu86O9Z5TYi5colXDmvhYy7vEEVg5Jt7bR+IzHzn5jDnsp+syF6TM0ThnBccThWZ+8o7Ef6oWQ==AVSGTjq7zTmDRqwRx1miiPNIEl6lkK7+KQC/wpy+3Pm08Vr7nK1n7gEWa6a60GIBrl976tfgr3P31MPtFzoJk+dBouobF62+8gQWq9JdfkfCm8cmm+CKFWH2/4aaxqpIrpNYq0zz5OVKhZZrYC/ZnzVTwyE0Q3U9bx/jRILOtPgBVdcTiaNVEg/WUm+gZw/QxL5EiLS45b6Yuy55XtcDAsAUHe91w+akD+ruPLAaZRa+5YU/6gtHWVeJxKsDl9NPozfa0bRpAelPLtinmweKBDqXcEr7JTRGP0VBk9OA/7ftVDcHVRCt4Bv0hZcOoFmZpsOAepm3ejQfG4BHskfEGOBPEszghwzNoWzJsk0bJZAJJS5lX1CmQyd48dnHcrEl0IoaA8XwFRTu4ockDYcPYfksYBUVh3N/SJRXhl7Uk8V9gIfs3IW8iu8d7fjUWYDtIRBbMW9YWwjXXGA+mZPYmTIsxIhI/XBOQo7pFbaHP8xRY7w7t1sGb7q9Z7Mf+h+VFJY3hrLFAoTYJEqb4wwK",
        //         Output = "{\"status\": \"success\", \"arnEndpoint\": \"" +"arn:aws:sns:eu-central-1:803864580155:endpoint/GCM/AndroidHelmet/b95a9a22-bb8a-3efe-850c-51773c7c7d5d" +"\", \"user_id\": \"" 
        //                  + "30a9433b-7a3c-4b3a-8f2f-2e238a17c078" +"\", \"folder\": \"" + "dva-08.17.2023.07.16-457c3fd8-1b53-44f4-b44a-fa75981d2be2" +"\"}"
        //     });
        // }
        // else {
        //     _stepClient.SendTaskFailureAsync(new SendTaskFailureRequest {
        //         TaskToken = "AQCMAAAAKgAAAAMAAAAAAAAAAfZ0dqbz64dbY/CbLHn71Ej+NXLmT7PDr1egBngurwF/iu86O9Z5TYi5colXDmvhYy7vEEVg5Jt7bR+IzHzn5jDnsp+syF6TM0ThnBccThWZ+8o7Ef6oWQ==AVSGTjq7zTmDRqwRx1miiPNIEl6lkK7+KQC/wpy+3Pm08Vr7nK1n7gEWa6a60GIBrl976tfgr3P31MPtFzoJk+dBouobF62+8gQWq9JdfkfCm8cmm+CKFWH2/4aaxqpIrpNYq0zz5OVKhZZrYC/ZnzVTwyE0Q3U9bx/jRILOtPgBVdcTiaNVEg/WUm+gZw/QxL5EiLS45b6Yuy55XtcDAsAUHe91w+akD+ruPLAaZRa+5YU/6gtHWVeJxKsDl9NPozfa0bRpAelPLtinmweKBDqXcEr7JTRGP0VBk9OA/7ftVDcHVRCt4Bv0hZcOoFmZpsOAepm3ejQfG4BHskfEGOBPEszghwzNoWzJsk0bJZAJJS5lX1CmQyd48dnHcrEl0IoaA8XwFRTu4ockDYcPYfksYBUVh3N/SJRXhl7Uk8V9gIfs3IW8iu8d7fjUWYDtIRBbMW9YWwjXXGA+mZPYmTIsxIhI/XBOQo7pFbaHP8xRY7w7t1sGb7q9Z7Mf+h+VFJY3hrLFAoTYJEqb4wwK",
        //         Cause = "No model found",
        //         Error = "No model found"
        //     });
        // }
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

    private static bool IsModelInDirectory() {
        
        DirectoryInfo di = new DirectoryInfo(PATH + "Models");
        foreach (FileInfo file in di.GetFiles()) {
            if (file.Extension == ".fbx") {
                return true;
            }
        }

        return false;
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
        startInfo.FileName = PATH + "/Scripts/HeadReconstruction.bat";
        startInfo.WorkingDirectory = PATH;
        
        var rc = Process.Start(startInfo);
        rc?.WaitForExit();

        if (IsModelInDirectory()) {
            _s3TransferUtility.UploadDirectory(
                PATH + "Models",
                "finishedhelmetmodels/"+ deser["Folder"]
            );
        
            _stepClient.SendTaskSuccessAsync(new SendTaskSuccessRequest {
                TaskToken = deser["TaskToken"],
                Output = "{\"status\": \"success\", \"arnEndpoint\": \"" + deser["ARN"] +"\", \"user_id\": \"" 
                         + deser["UserID"] +"\", \"folder\": \"" + deser["Folder"] +"\"}"
            });
        }
        else {
            _stepClient.SendTaskFailureAsync(new SendTaskFailureRequest {
                TaskToken = deser["TaskToken"],
                Cause = "{\"status\": \"success\", \"arnEndpoint\": \"" + deser["ARN"] +"\", \"user_" +
                        "id\": \"" 
                         + deser["UserID"] +"\", \"folder\": \"" + deser["Folder"] +"\"}",
                
            });
        }
        
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