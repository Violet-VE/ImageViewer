﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using ImageFramework.Annotations;
using ImageFramework.ImageLoader;

namespace ImageFramework.Model
{
    /// <summary>
    /// gives information about image pipelines in progress
    /// </summary>
    public class ProgressModel : INotifyPropertyChanged, IDisposable
    {
        private float progress = 0.0f;
        private readonly Dll.ProgressDelegate onDllProgress;
        private Task currentTask = null;
        private CancellationTokenSource currentTaskCancellation = null;

        public ProgressModel()
        {
            // set dll progress
            onDllProgress = OnDllProgress;
            Dll.set_progress_callback(onDllProgress);
        }

        private uint OnDllProgress(float prog, string description)
        {
            if (!IsProcessing) return 0; // ignore for now => progress when opening files without task

            Progress = prog;
            if (What != description)
                What = description;

            if (currentTaskCancellation.IsCancellationRequested)
                return 1;

            return 0;
        }

        /// <summary>
        /// progress between 0.0 and 1.0
        /// </summary>
        public float Progress
        {
            get => progress;
            internal set
            {
                float clamped = Math.Min(Math.Max(value, 0.0f), 1.0f);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (clamped == progress) return;
                progress = clamped;
                OnPropertyChanged(nameof(Progress));
            }
        }



        private string what = "";

        /// <summary>
        /// description of the thing being processed
        /// </summary>
        public string What
        {
            get => what;
            internal set
            {
                var val = value ?? "";
                //if (val.Equals(what)) return;
                what = val;
                OnPropertyChanged(nameof(What));
            }
        }

        private string lastError = "";

        public string LastError
        {
            get => lastError;
            private set
            {
                if (value == lastError) return;
                lastError = value;
                OnPropertyChanged(nameof(LastError));
            }
        }

        /// <summary>
        /// indicates if anything is being processed
        /// </summary>
        public bool IsProcessing => currentTask != null;


        internal void AddTask(Task t, CancellationTokenSource cts)
        {
            if(currentTask != null)
                WaitForTask();

            currentTask = t;
            currentTaskCancellation = cts;
            LastError = "";
            Progress = 0.0f;
            What = "";
            // automatically unregister task when finished or failed
            t.ContinueWith(OnTaskFinished);
            OnPropertyChanged(nameof(IsProcessing));
        }

        /// waits until the active task has finished
        public void WaitForTask()
        {
            try
            {
                currentTask?.Wait();
            }
            catch (Exception)
            {
                // this is handled by OnTaskFinished
            }
        }

        /// returns a task that waits for the active task to finish.
        /// This is guaranteed to never throw an exception
        public async Task WaitForTaskAsync()
        {
            if (currentTask == null) return;
            try
            {
                await currentTask;
            }
            catch (Exception)
            {
                // this is handled by OnTaskFinished
            }
        }

        // callback that will be executed after the active task finished
        private void OnTaskFinished(Task prevTask)
        {
            currentTask = null;
            currentTaskCancellation.Dispose();
            currentTaskCancellation = null;
            OnPropertyChanged(nameof(IsProcessing));

            if (prevTask.Exception == null) return;
            LastError = prevTask.Exception.Message;
        }

        // cancels the current task
        public void Cancel()
        {
            currentTaskCancellation?.Cancel();
            WaitForTask();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            Cancel();
            Debug.Assert(currentTask == null);
            Debug.Assert(currentTaskCancellation == null);
        }
    }
}
