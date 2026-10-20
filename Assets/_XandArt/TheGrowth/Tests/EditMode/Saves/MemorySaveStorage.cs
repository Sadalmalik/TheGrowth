using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using TheGrowth.Architecture.IOC;
using TheGrowth.Core.Saves;
using TheGrowth.Tests.EditMode.Scripting;

namespace TheGrowth.Tests.EditMode.Saves
{
    /// <summary>
    /// Хранилище в памяти: слот — массив байт, запись атомарна по построению. Завершается синхронно; <see cref="WriteGate"/>
    /// задерживает каждую запись до <see cref="Gate.Open"/>. Потоки чтения считают прочитанные байты (<see cref="Opened"/>).
    /// </summary>
    public sealed class MemorySaveStorage : SharedObject, ISaveStorage
    {
        public readonly Dictionary<string, byte[]> Slots = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        public readonly List<ReadTrackingStream> Opened = new List<ReadTrackingStream>();

        public Gate WriteGate { get; set; }

        public int WriteCount { get; private set; }

        public UniTask<IReadOnlyList<string>> ListAsync(CancellationToken token)
        {
            return UniTask.FromResult<IReadOnlyList<string>>(new List<string>(Slots.Keys));
        }

        public UniTask<Stream> OpenReadAsync(string slotId, CancellationToken token)
        {
            if (!Slots.TryGetValue(slotId, out var bytes))
                return UniTask.FromResult<Stream>(null);

            var stream = new ReadTrackingStream(new MemoryStream(bytes, false));
            Opened.Add(stream);
            return UniTask.FromResult<Stream>(stream);
        }

        public async UniTask WriteAsync(string slotId, Action<Stream> write, CancellationToken token)
        {
            if (WriteGate != null)
                await WriteGate.Wait(token);

            using (var buffer = new MemoryStream())
            {
                write(buffer);
                Slots[slotId] = buffer.ToArray();
            }

            WriteCount++;
        }

        public UniTask DeleteAsync(string slotId, CancellationToken token)
        {
            Slots.Remove(slotId);
            return UniTask.CompletedTask;
        }
    }

    /// <summary>Поток-обёртка только для чтения: сколько байт прочитано (проверка «список слотов читает только meta»).</summary>
    public sealed class ReadTrackingStream : Stream
    {
        private readonly Stream m_Inner;

        public ReadTrackingStream(Stream inner)
        {
            m_Inner = inner;
        }

        public long BytesRead { get; private set; }

        public override bool CanRead => m_Inner.CanRead;

        public override bool CanSeek => m_Inner.CanSeek;

        public override bool CanWrite => false;

        public override long Length => m_Inner.Length;

        public override long Position
        {
            get => m_Inner.Position;
            set => m_Inner.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = m_Inner.Read(buffer, offset, count);
            BytesRead += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_Inner.Seek(offset, origin);
        }

        public override void Flush()
        {
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                m_Inner.Dispose();

            base.Dispose(disposing);
        }
    }
}
