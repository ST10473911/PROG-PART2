using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Globalization;

namespace CybersecurityChatbotGUI
{
    public class Chatbot
    {
        // Part 1 & 2 Features
        private string userName;
        private string userInterest;
        private string lastTopic;
        private Random random = new Random();
        private SpeechSynthesizer speechSynthesizer;
        private Dictionary<string, List<string>> keywordResponses;
        private Dictionary<string, string> followUpResponses;
        private Dictionary<string, string> sentimentResponses;

        // Part 3 - Task List (for display, DB for storage)
        private List<TaskItem> tasks = new List<TaskItem>();

        // Part 3 - Activity Log
        private List<string> activityLog = new List<string>();
        private const int MaxLogEntries = 10;

        // Part 3 - Quiz
        private List<QuizQuestion> quizQuestions;
        private int currentQuestionIndex = -1;
        private int quizScore = 0;
        private bool quizActive = false;

        // Part 3 - NLP Patterns
        private Dictionary<string, List<string>> nlpPatterns;
        private Dictionary<string, string> nlpIntents;

        // Database connection
        private string connectionString;

        public Chatbot()
        {
            InitializeResponses();
            InitializeSentimentResponses();
            InitializeNLP();
            InitializeQuizQuestions();
            speechSynthesizer = new SpeechSynthesizer();

            // Get connection string from App.config
            connectionString = ConfigurationManager.ConnectionStrings["MySqlConnection"].ConnectionString;

            // Load tasks from database
            LoadTasksFromDatabase();

            // Add initial activity log entry
            AddActivityLog("Chatbot initialized and ready");
        }

        // ============ PART 1 & 2 - EXISTING CODE ============
        private void InitializeResponses()
        {
            keywordResponses = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["password"] = new List<string>
                {
                    "🔐 Create strong passwords with at least 12 characters including uppercase, lowercase, numbers, and symbols.",
                    "🔑 Never reuse passwords across different accounts. Use a password manager!",
                    "🛡️ Enable two-factor authentication whenever possible for extra security."
                },
                ["scan"] = new List<string>
                {
                    "📱 Regularly scan your devices for malware using trusted antivirus software.",
                    "🔍 Before downloading files, scan them with Windows Defender first.",
                    "✅ Run weekly security scans to detect and remove potential threats."
                },
                ["privacy"] = new List<string>
                {
                    "👁️ Review privacy settings on social media to control who sees your information.",
                    "🔒 Use a VPN when using public Wi-Fi to protect your personal data.",
                    "📧 Avoid sharing personal information like your ID number or address in emails."
                },
                ["phishing"] = new List<string>
                {
                    "🎣 Never click on suspicious links in emails or text messages.",
                    "⚠️ Check the sender's email address carefully - scammers use fake addresses.",
                    "🔗 Hover over links before clicking to see the actual URL destination."
                },
                ["how are you"] = new List<string>
                {
                    "I'm doing great! How can I help you with cybersecurity today?",
                    "I'm fully secure and ready to assist you!",
                    "Feeling safe and sound! What would you like to learn about?"
                },
                ["what's your purpose"] = new List<string>
                {
                    "My purpose is to educate South African citizens about staying safe online!",
                    "I'm here to help you learn about cybersecurity threats and how to avoid them.",
                    "Think of me as your personal cybersecurity assistant!"
                },
                ["what can i ask you about"] = new List<string>
                {
                    "You can ask me about: passwords, scanning, privacy, phishing, safe browsing, tasks, quiz, or activity log!",
                    "Try asking about 'password safety', 'phishing tips', or 'privacy'. Also try 'add task', 'start quiz', or 'show log'.",
                    "I can help with cybersecurity topics, manage your tasks, test your knowledge with a quiz, and track activity."
                },
                ["help"] = new List<string>
                {
                    "Available topics: password, scan, privacy, phishing.\nCommands: 'add task', 'show tasks', 'start quiz', 'show log'.\nSay 'my name is [name]' so I remember you!",
                    "Type 'password', 'scan', 'privacy', or 'phishing' to learn.\nTry 'add task' to create a reminder.\nTry 'start quiz' to test your knowledge!\nI remember your name and interests too."
                }
            };

            followUpResponses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["tell me more"] = "Here's more information about {topic}:",
                ["another tip"] = "Another helpful tip about {topic}:",
                ["explain more"] = "Let me explain further about {topic}:"
            };
        }

        private void InitializeSentimentResponses()
        {
            sentimentResponses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["worried"] = "It's completely normal to feel worried about online security. Don't worry - I'm here to help! Here's a tip:",
                ["scared"] = "I understand your concern. Let me share something that will help you feel safer:",
                ["frustrated"] = "I hear your frustration. Let me simplify this for you:",
                ["curious"] = "That's great that you're curious! Here's what you should know:",
                ["confused"] = "Let me explain this in simpler terms:"
            };
        }

        // ============ PART 3 - NLP (Task 3) ============
        private void InitializeNLP()
        {
            nlpIntents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["addtask"] = "add_task",
                ["remind"] = "add_task",
                ["createtask"] = "add_task",
                ["showtasks"] = "view_tasks",
                ["listtasks"] = "view_tasks",
                ["viewtasks"] = "view_tasks",
                ["mytasks"] = "view_tasks",
                ["completetask"] = "complete_task",
                ["donetask"] = "complete_task",
                ["finishtask"] = "complete_task",
                ["deletetask"] = "delete_task",
                ["removetask"] = "delete_task",
                ["startquiz"] = "start_quiz",
                ["takequiz"] = "start_quiz",
                ["beginquiz"] = "start_quiz",
                ["playquiz"] = "start_quiz",
                ["showlog"] = "show_log",
                ["activitylog"] = "show_log",
                ["history"] = "show_log",
                ["what have you done"] = "show_log",
                ["what did you do"] = "show_log"
            };

            nlpPatterns = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["add_task"] = new List<string>
                {
                    @"add (?:a )?task (?:to )?(.+)",
                    @"remind me (?:to )?(.+)",
                    @"create (?:a )?task (?:to )?(.+)",
                    @"set (?:a )?reminder (?:to )?(.+)"
                },
                ["view_tasks"] = new List<string>
                {
                    @"show (?:my )?tasks",
                    @"list (?:my )?tasks",
                    @"view (?:my )?tasks"
                },
                ["complete_task"] = new List<string>
                {
                    @"complete (?:task )?(.+)",
                    @"done (?:with )?(.+)",
                    @"finish (?:task )?(.+)"
                },
                ["delete_task"] = new List<string>
                {
                    @"delete (?:task )?(.+)",
                    @"remove (?:task )?(.+)",
                    @"cancel (?:task )?(.+)"
                }
            };
        }

        private string GetIntent(string input)
        {
            foreach (var intent in nlpIntents)
            {
                if (input.Contains(intent.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return intent.Value;
                }
            }

            // Check patterns
            foreach (var patternGroup in nlpPatterns)
            {
                foreach (string pattern in patternGroup.Value)
                {
                    if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
                    {
                        return patternGroup.Key;
                    }
                }
            }

            return null;
        }

        // ============ PART 3 - DATABASE TASKS (Task 1) ============
        public class TaskItem
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public DateTime? ReminderDate { get; set; }
            public bool IsCompleted { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class QuizQuestion
        {
            public string Question { get; set; }
            public List<string> Options { get; set; }
            public int CorrectAnswerIndex { get; set; }
            public string Explanation { get; set; }
            public string Topic { get; set; }
        }

        private void LoadTasksFromDatabase()
        {
            try
            {
                tasks.Clear();
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id, title, description, reminder_date, is_completed, created_at FROM tasks ORDER BY created_at DESC";
                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new TaskItem
                            {
                                Id = reader.GetInt32("id"),
                                Title = reader.GetString("title"),
                                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                                ReminderDate = reader.IsDBNull(reader.GetOrdinal("reminder_date")) ? (DateTime?)null : reader.GetDateTime("reminder_date"),
                                IsCompleted = reader.GetBoolean("is_completed"),
                                CreatedAt = reader.GetDateTime("created_at")
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddActivityLog($"Database load error: {ex.Message}");
            }
        }

        private void SaveTaskToDatabase(TaskItem task)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO tasks (title, description, reminder_date, is_completed) 
                                    VALUES (@title, @description, @reminder_date, @is_completed)";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@title", task.Title);
                        cmd.Parameters.AddWithValue("@description", (object)task.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@reminder_date", (object)task.ReminderDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@is_completed", task.IsCompleted);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadTasksFromDatabase(); // Refresh
            }
            catch (Exception ex)
            {
                AddActivityLog($"Database save error: {ex.Message}");
            }
        }

        private void UpdateTaskInDatabase(TaskItem task)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"UPDATE tasks SET title=@title, description=@description, reminder_date=@reminder_date, is_completed=@is_completed 
                                    WHERE id=@id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", task.Id);
                        cmd.Parameters.AddWithValue("@title", task.Title);
                        cmd.Parameters.AddWithValue("@description", (object)task.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@reminder_date", (object)task.ReminderDate ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@is_completed", task.IsCompleted);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadTasksFromDatabase(); // Refresh
            }
            catch (Exception ex)
            {
                AddActivityLog($"Database update error: {ex.Message}");
            }
        }

        private void DeleteTaskFromDatabase(int taskId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM tasks WHERE id=@id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", taskId);
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadTasksFromDatabase(); // Refresh
            }
            catch (Exception ex)
            {
                AddActivityLog($"Database delete error: {ex.Message}");
            }
        }

        // ============ PART 3 - QUIZ (Task 2) ============
        private void InitializeQuizQuestions()
        {
            quizQuestions = new List<QuizQuestion>
            {
                // 15 questions to exceed 10 requirement
                new QuizQuestion
                {
                    Question = "What should you do if you receive an email asking for your password?",
                    Options = new List<string> { "Reply with your password", "Delete the email", "Report the email as phishing", "Ignore it" },
                    CorrectAnswerIndex = 2,
                    Explanation = "Reporting phishing emails helps prevent scams and protects others.",
                    Topic = "phishing"
                },
                new QuizQuestion
                {
                    Question = "Which of the following is a strong password?",
                    Options = new List<string> { "password123", "12345678", "Blue-Horse-Shoe-Coffee!", "qwerty" },
                    CorrectAnswerIndex = 2,
                    Explanation = "A strong password uses a mix of uppercase, lowercase, numbers, and symbols.",
                    Topic = "password"
                },
                new QuizQuestion
                {
                    Question = "True or False: You should use the same password for all your accounts.",
                    Options = new List<string> { "True", "False" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Using the same password everywhere is dangerous. If one account is hacked, all are compromised.",
                    Topic = "password"
                },
                new QuizQuestion
                {
                    Question = "What is two-factor authentication (2FA)?",
                    Options = new List<string> { "Using two different passwords", "A second layer of security", "A type of virus", "An email filter" },
                    CorrectAnswerIndex = 1,
                    Explanation = "2FA adds an extra layer of protection by requiring a second verification step.",
                    Topic = "password"
                },
                new QuizQuestion
                {
                    Question = "True or False: Public Wi-Fi is always safe to use for banking.",
                    Options = new List<string> { "True", "False" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Public Wi-Fi is often unsecured. Use a VPN for sensitive transactions.",
                    Topic = "browsing"
                },
                new QuizQuestion
                {
                    Question = "What is phishing?",
                    Options = new List<string> { "A type of fish", "A scam to steal personal information", "A computer virus", "A password manager" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Phishing scams trick people into revealing sensitive information.",
                    Topic = "phishing"
                },
                new QuizQuestion
                {
                    Question = "True or False: You should click on links in emails from unknown senders.",
                    Options = new List<string> { "True", "False" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Unknown senders may be scammers trying to install malware or steal information.",
                    Topic = "phishing"
                },
                new QuizQuestion
                {
                    Question = "What should you do to protect your privacy on social media?",
                    Options = new List<string> { "Share everything publicly", "Review privacy settings", "Never use social media", "Post your address" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Reviewing and adjusting privacy settings controls who can see your information.",
                    Topic = "privacy"
                },
                new QuizQuestion
                {
                    Question = "True or False: Antivirus software is optional for cybersecurity.",
                    Options = new List<string> { "True", "False" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Antivirus software is essential for protecting against malware and viruses.",
                    Topic = "scan"
                },
                new QuizQuestion
                {
                    Question = "What is social engineering?",
                    Options = new List<string> { "A type of virus", "Manipulating people to reveal information", "A password cracker", "A privacy setting" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Social engineering tricks people into giving up confidential information.",
                    Topic = "phishing"
                },
                new QuizQuestion
                {
                    Question = "True or False: You should share your OTP (one-time password) with bank employees.",
                    Options = new List<string> { "True", "False" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Banks will NEVER ask for your OTP. This is always a scam.",
                    Topic = "password"
                },
                new QuizQuestion
                {
                    Question = "What should you do before downloading software?",
                    Options = new List<string> { "Click the first link", "Scan it with antivirus", "Ignore it", "Share it with friends" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Always scan downloaded files to prevent malware installation.",
                    Topic = "scan"
                },
                new QuizQuestion
                {
                    Question = "True or False: A VPN protects your online privacy.",
                    Options = new List<string> { "True", "False" },
                    CorrectAnswerIndex = 0,
                    Explanation = "A VPN encrypts your internet traffic and hides your IP address.",
                    Topic = "privacy"
                },
                new QuizQuestion
                {
                    Question = "How often should you change your passwords?",
                    Options = new List<string> { "Every week", "Regularly or when compromised", "Never", "Once a year" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Regular password changes, especially after a breach, help maintain security.",
                    Topic = "password"
                },
                new QuizQuestion
                {
                    Question = "True or False: Urgent emails requesting personal information should be trusted.",
                    Options = new List<string> { "True", "False" },
                    CorrectAnswerIndex = 1,
                    Explanation = "Scammers often create urgency to pressure you into making mistakes.",
                    Topic = "phishing"
                }
            };
        }

        // ============ PART 3 - ACTIVITY LOG (Task 4) ============
        private void AddActivityLog(string action)
        {
            string logEntry = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm")} - {action}";
            activityLog.Insert(0, logEntry);
            if (activityLog.Count > MaxLogEntries)
            {
                activityLog.RemoveAt(activityLog.Count - 1);
            }
        }

        // ============ MAIN PROCESS METHOD ============
        public string ProcessInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "Please type a message so I can help you with cybersecurity!";
            }

            // Activity log: User input
            AddActivityLog($"User input: {input}");

            // --- Check for Quiz answers first ---
            if (quizActive && currentQuestionIndex >= 0 && currentQuestionIndex < quizQuestions.Count)
            {
                string quizResult = ProcessQuizAnswer(input);
                if (quizResult != null)
                {
                    AddActivityLog($"Quiz answer processed");
                    return quizResult;
                }
            }

            // --- Part 3: NLP Intent Detection ---
            string intent = GetIntent(input);
            if (intent != null)
            {
                switch (intent)
                {
                    case "add_task":
                        return HandleAddTask(input);
                    case "view_tasks":
                        return HandleViewTasks();
                    case "complete_task":
                        return HandleCompleteTask(input);
                    case "delete_task":
                        return HandleDeleteTask(input);
                    case "start_quiz":
                        return HandleStartQuiz();
                    case "show_log":
                        return HandleShowLog();
                }
            }

            // --- Part 1 & 2: Existing Logic ---

            // Sentiment detection
            string sentiment = DetectSentiment(input);
            if (sentiment != null && sentimentResponses.ContainsKey(sentiment))
            {
                string response = sentimentResponses[sentiment] + " " + GetRandomCybersecurityTip();
                AddActivityLog($"Sentiment detected: {sentiment}");
                return response;
            }

            // Name memory
            if (string.IsNullOrEmpty(userName))
            {
                string extractedName = ExtractName(input);
                if (extractedName != null)
                {
                    userName = extractedName;
                    AddActivityLog($"User name remembered: {userName}");
                    return $"Nice to meet you, {userName}! I'll remember that. What would you like to learn about today?";
                }
            }

            // Interest memory
            string interest = ExtractInterest(input);
            if (interest != null)
            {
                userInterest = interest;
                AddActivityLog($"User interest remembered: {userInterest}");
                return $"Great! I'll remember that you're interested in {interest}. It's very important for staying safe online!";
            }

            // Follow-up questions
            string followUpResponse = HandleFollowUp(input);
            if (followUpResponse != null)
            {
                return followUpResponse;
            }

            // Keyword recognition
            string keywordResponse = GetKeywordResponse(input);
            if (keywordResponse != null)
            {
                lastTopic = keywordResponse;
                return keywordResponse;
            }

            // Recall memory
            if (input.Contains("my name") || input.Contains("who am i"))
            {
                return !string.IsNullOrEmpty(userName) ? $"You told me your name is {userName}!" : "I don't know your name yet. What should I call you?";
            }

            if (input.Contains("my interest") || input.Contains("interested in"))
            {
                return !string.IsNullOrEmpty(userInterest) ? $"You're interested in {userInterest}. Would you like to learn more?" : "You haven't told me what cybersecurity topic interests you yet.";
            }

            // Default response
            return "I'm not sure I understand. You can ask me about passwords, scanning, privacy, or phishing. Try 'help' for all options, 'add task', 'start quiz', or 'show log'.";
        }

        // ============ PART 3 - TASK HANDLERS ============
        private string HandleAddTask(string input)
        {
            // Extract task description
            string taskTitle = ExtractTaskDescription(input);

            if (string.IsNullOrWhiteSpace(taskTitle))
            {
                return "I didn't catch what task you want to add. Try: 'Add task: Review privacy settings' or 'Remind me to enable 2FA'";
            }

            // Check if user wants a reminder
            var reminderMatch = Regex.Match(input, @"remind(?:er)? (?:me )?in (\d+) (day|days|week|weeks|month|months)", RegexOptions.IgnoreCase);
            DateTime? reminderDate = null;

            if (reminderMatch.Success)
            {
                int count = int.Parse(reminderMatch.Groups[1].Value);
                string unit = reminderMatch.Groups[2].Value.ToLower();

                if (unit.StartsWith("day"))
                    reminderDate = DateTime.Now.AddDays(count);
                else if (unit.StartsWith("week"))
                    reminderDate = DateTime.Now.AddDays(count * 7);
                else if (unit.StartsWith("month"))
                    reminderDate = DateTime.Now.AddMonths(count);
            }
            else if (input.Contains("reminder") || input.Contains("remind"))
            {
                return $"Task '{taskTitle}' added! Would you like a reminder? (Say 'remind me in X days')";
            }

            // Save to database
            var task = new TaskItem
            {
                Title = taskTitle,
                Description = $"Cybersecurity task: {taskTitle}",
                ReminderDate = reminderDate,
                IsCompleted = false
            };

            SaveTaskToDatabase(task);
            AddActivityLog($"Task added: '{taskTitle}'" + (reminderDate.HasValue ? $" (Reminder: {reminderDate.Value.ToShortDateString()})" : ""));

            string response = $"✅ Task added: '{taskTitle}'";
            if (reminderDate.HasValue)
            {
                response += $"\n🔔 I'll remind you on {reminderDate.Value.ToShortDateString()}!";
            }
            else
            {
                response += "\n💡 You can say 'show tasks' to see all your tasks.";
            }

            return response;
        }

        private string ExtractTaskDescription(string input)
        {
            // Try to extract after "add task", "remind me", "create task", etc.
            string[] patterns = {
                @"add (?:a )?task (?:to |: )?(.+)",
                @"remind me (?:to |: )?(.+)",
                @"create (?:a )?task (?:to |: )?(.+)",
                @"set (?:a )?reminder (?:to |: )?(.+)",
                @"(?:please )?remind me (?:to |: )?(.+)"
            };

            foreach (string pattern in patterns)
            {
                var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string result = match.Groups[1].Value.Trim();
                    // Remove "tomorrow", "in X days", etc. from the task
                    result = Regex.Replace(result, @"tomorrow|in \d+ days?|in \d+ weeks?|in \d+ months?", "", RegexOptions.IgnoreCase).Trim();
                    return result;
                }
            }

            // If no pattern matches, return the whole input
            return input.Trim();
        }

        private string HandleViewTasks()
        {
            LoadTasksFromDatabase(); // Refresh from DB

            if (tasks.Count == 0)
            {
                return "📝 You have no cybersecurity tasks yet. Try saying 'Add task: Enable 2FA' to create one!";
            }

            string response = "📋 **Your Cybersecurity Tasks:**\n";
            int count = 1;

            foreach (var task in tasks)
            {
                string status = task.IsCompleted ? "✅" : "⏳";
                string reminder = task.ReminderDate.HasValue ? $" (Reminder: {task.ReminderDate.Value.ToShortDateString()})" : "";
                response += $"{count}. {status} {task.Title}{reminder}\n";
                count++;
            }

            response += "\n💡 Say 'complete task [number]' to mark as done, or 'delete task [number]' to remove it.";
            return response;
        }

        private string HandleCompleteTask(string input)
        {
            var match = Regex.Match(input, @"(?:complete|done|finish) (?:task )?(\d+)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return "Please specify the task number. Example: 'complete task 1' or 'done with task 2'";
            }

            int taskIndex = int.Parse(match.Groups[1].Value) - 1;
            if (taskIndex < 0 || taskIndex >= tasks.Count)
            {
                return "Task number not found. Say 'show tasks' to see your tasks.";
            }

            var task = tasks[taskIndex];
            task.IsCompleted = true;
            UpdateTaskInDatabase(task);
            AddActivityLog($"Task completed: '{task.Title}'");

            return $"✅ Task '{task.Title}' marked as completed! Well done staying on top of your cybersecurity!";
        }

        private string HandleDeleteTask(string input)
        {
            var match = Regex.Match(input, @"(?:delete|remove|cancel) (?:task )?(\d+)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return "Please specify the task number. Example: 'delete task 1'";
            }

            int taskIndex = int.Parse(match.Groups[1].Value) - 1;
            if (taskIndex < 0 || taskIndex >= tasks.Count)
            {
                return "Task number not found. Say 'show tasks' to see your tasks.";
            }

            var task = tasks[taskIndex];
            DeleteTaskFromDatabase(task.Id);
            AddActivityLog($"Task deleted: '{task.Title}'");

            return $"🗑️ Task '{task.Title}' has been deleted.";
        }

        // ============ PART 3 - QUIZ HANDLERS ============
        private string HandleStartQuiz()
        {
            if (quizActive)
            {
                return "You're already in a quiz! Answer the current question or say 'quit quiz' to exit.";
            }

            quizActive = true;
            currentQuestionIndex = -1;
            quizScore = 0;
            AddActivityLog("Quiz started");

            // Shuffle questions
            quizQuestions = quizQuestions.OrderBy(x => random.Next()).ToList();
            currentQuestionIndex = 0;
            return GetQuestionText();
        }

        private string GetQuestionText()
        {
            if (currentQuestionIndex < 0 || currentQuestionIndex >= quizQuestions.Count)
            {
                return "No more questions available.";
            }

            var question = quizQuestions[currentQuestionIndex];
            string response = $"📝 **Question {currentQuestionIndex + 1} of {quizQuestions.Count}**\n";
            response += $"Topic: {question.Topic}\n\n";
            response += $"{question.Question}\n\n";

            for (int i = 0; i < question.Options.Count; i++)
            {
                response += $"{(char)('A' + i)}) {question.Options[i]}\n";
            }

            response += "\nType your answer (A, B, C, D or True/False):";
            return response;
        }

        private string ProcessQuizAnswer(string input)
        {
            if (input.Contains("quit") || input.Contains("exit"))
            {
                quizActive = false;
                AddActivityLog("Quiz exited early");
                return $"Quiz ended. You completed {quizScore} questions correctly. Keep learning!";
            }

            var question = quizQuestions[currentQuestionIndex];
            int answerIndex = -1;

            // Parse answer
            string trimmed = input.Trim().ToUpper();
            if (trimmed == "A" || trimmed == "A)" || trimmed == "TRUE") answerIndex = 0;
            else if (trimmed == "B" || trimmed == "B)" || trimmed == "FALSE") answerIndex = 1;
            else if (trimmed == "C" || trimmed == "C)") answerIndex = 2;
            else if (trimmed == "D" || trimmed == "D)") answerIndex = 3;

            // If not matching, check if input matches any option text (for True/False)
            if (answerIndex == -1)
            {
                for (int i = 0; i < question.Options.Count; i++)
                {
                    if (question.Options[i].Equals(input.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        answerIndex = i;
                        break;
                    }
                }
            }

            if (answerIndex == -1)
            {
                return $"Please answer with the letter (A, B, C, D) or 'True'/'False'.\n\n{GetQuestionText()}";
            }

            bool isCorrect = answerIndex == question.CorrectAnswerIndex;
            if (isCorrect) quizScore++;

            string feedback = isCorrect ? "✅ Correct!" : $"❌ Incorrect. The correct answer was {question.Options[question.CorrectAnswerIndex]}.";
            feedback += $"\n💡 {question.Explanation}\n";

            // Move to next question
            currentQuestionIndex++;

            if (currentQuestionIndex >= quizQuestions.Count)
            {
                quizActive = false;
                AddActivityLog($"Quiz completed with {quizScore}/{quizQuestions.Count} correct");

                string performance = quizScore >= quizQuestions.Count * 0.7
                    ? "🌟 Great job! You're a cybersecurity pro! 🎉"
                    : "📚 Keep learning to stay safe online! You can retake the quiz anytime.";

                return $"{feedback}\n\n🎯 **Quiz Complete!**\nScore: {quizScore}/{quizQuestions.Count}\n{performance}\n\nSay 'start quiz' to try again!";
            }

            return $"{feedback}\n\n{GetQuestionText()}";
        }

        // ============ PART 3 - ACTIVITY LOG ============
        private string HandleShowLog()
        {
            if (activityLog.Count == 0)
            {
                return "📋 No activities logged yet. Start adding tasks, taking quizzes, or asking questions!";
            }

            string response = "📋 **Recent Activity Log:**\n";
            response += "━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";

            for (int i = 0; i < Math.Min(activityLog.Count, 10); i++)
            {
                response += $"{i + 1}. {activityLog[i]}\n";
            }

            if (activityLog.Count > 10)
            {
                response += $"\n(Showing last 10 of {activityLog.Count} entries)";
            }

            return response;
        }

        // ============ PART 1 & 2 - HELPER METHODS ============
        public void PlayVoiceGreeting()
        {
            try
            {
                string fullPath = "greeting.wav";
                if (System.IO.File.Exists(fullPath))
                {
                    using (var player = new SoundPlayer(fullPath))
                    {
                        player.PlaySync();
                    }
                }
                else
                {
                    speechSynthesizer.Speak("Hello! Welcome to the Cybersecurity Awareness Bot. I'm here to help you stay safe online.");
                }
            }
            catch (Exception) { }
        }

        public string GetAsciiArt()
        {
            return @"   ________  ___  ___  ___  ___  ________   
  |\   ____\|\  \|\  \|\  \|\  \|\   ____\  
  \ \  \___|\ \  \\\  \ \  \\\  \ \  \___|  
   \ \  \    \ \   __  \ \   __  \ \  \    
    \ \  \____\ \  \ \  \ \  \ \  \ \  \____
     \ \_______\ \__\ \__\ \__\ \__\ \_______\
      \|_______|\|__|\|__|\|__|\|__|\|_______|
      
              🔐 CYBERSECURITY BOT 🔐
           Protecting South African citizens";
        }

        private string GetKeywordResponse(string input)
        {
            foreach (var keyword in keywordResponses.Keys)
            {
                if (input.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    List<string> responses = keywordResponses[keyword];
                    return responses[random.Next(responses.Count)];
                }
            }
            return null;
        }

        private string HandleFollowUp(string input)
        {
            if (string.IsNullOrEmpty(lastTopic)) return null;

            foreach (var followUp in followUpResponses.Keys)
            {
                if (input.Contains(followUp, StringComparison.OrdinalIgnoreCase))
                {
                    var topicKey = keywordResponses.Keys.FirstOrDefault(k =>
                        lastTopic.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                        k.Contains(lastTopic, StringComparison.OrdinalIgnoreCase));
                    if (topicKey != null && keywordResponses.ContainsKey(topicKey))
                    {
                        var responses = keywordResponses[topicKey];
                        return followUpResponses[followUp].Replace("{topic}", topicKey) + " " + responses[random.Next(responses.Count)];
                    }
                    return $"Let me give you more information. {GetRandomCybersecurityTip()}";
                }
            }
            return null;
        }

        private string DetectSentiment(string input)
        {
            if (Regex.IsMatch(input, @"worried|anxious|nervous|concerned", RegexOptions.IgnoreCase)) return "worried";
            if (Regex.IsMatch(input, @"scared|terrified|fear|afraid", RegexOptions.IgnoreCase)) return "scared";
            if (Regex.IsMatch(input, @"frustrated|annoyed|angry", RegexOptions.IgnoreCase)) return "frustrated";
            if (Regex.IsMatch(input, @"curious|interested|want to learn", RegexOptions.IgnoreCase)) return "curious";
            if (Regex.IsMatch(input, @"confused|don't understand", RegexOptions.IgnoreCase)) return "confused";
            return null;
        }

        private string ExtractName(string input)
        {
            var match = Regex.Match(input, @"my name is (\w+)|call me (\w+)|i am (\w+)|i'm (\w+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value ?? match.Groups[2].Value ?? match.Groups[3].Value ?? match.Groups[4].Value;
            }
            return null;
        }

        private string ExtractInterest(string input)
        {
            string[] interests = { "password", "privacy", "phishing", "malware", "scanning" };
            foreach (string interest in interests)
            {
                if (Regex.IsMatch(input, $@"interested in {interest}|like {interest}|want to learn about {interest}", RegexOptions.IgnoreCase))
                {
                    return interest;
                }
            }
            return null;
        }

        private string GetRandomCybersecurityTip()
        {
            string[] tips = {
                "Always use strong, unique passwords for each account.",
                "Enable two-factor authentication wherever possible.",
                "Never share your passwords with anyone.",
                "Keep your software and operating system updated.",
                "Use antivirus software and keep it updated.",
                "Back up your important files regularly.",
                "Be cautious of unsolicited emails asking for personal information.",
                "Lock your computer screen when you step away from your desk."
            };
            return tips[random.Next(tips.Length)];
        }

        public string GetUserName() => userName ?? "Friend";
        public string GetUserInterest() => userInterest ?? "cybersecurity";

        // For UI to check if quiz is active
        public bool IsQuizActive() => quizActive;
        public int GetQuizProgress() => quizActive ? currentQuestionIndex : 0;
        public int GetTotalQuestions() => quizQuestions.Count;
        public int GetQuizScore() => quizScore;
    }
}
