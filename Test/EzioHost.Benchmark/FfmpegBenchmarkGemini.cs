using BenchmarkDotNet.Attributes;
using FFmpeg.AutoGen.Abstractions;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using System.Diagnostics;

namespace EzioHost.Benchmark
{
    // Removed MemoryDiagnoser and ThreadingDiagnoser attributes because they may load Visual Studio diagnostic agents not available in this environment
    public unsafe class FfmpegBenchmarkGemini
    {
        // Sử dụng Params để benchmark chạy trên mỗi file video
        //[Params("Videos/10s.mp4", "Videos/30s.mp4", "Videos/60s.mp4","Videos/full.mp4")]
        [Params("Videos/10s.mp4", "Videos/30s.mp4", "Videos/60s.mp4")]
        public string VideoPath { get; set; } = "Videos/Gachiakuta - E15.mp4";

        private const string OUTPUT_DIR = "Benchmark_output_gemini";
        private string _currentOutputDir = string.Empty;

        [GlobalSetup]
        public void Init()
        {
            DynamicallyLoadedBindings.Initialize();

            // Dọn dẹp thư mục output cũ nếu có
            if (Directory.Exists(OUTPUT_DIR))
            {
                Directory.Delete(OUTPUT_DIR, true);
            }
            Directory.CreateDirectory(OUTPUT_DIR);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Tạo một thư mục con duy nhất cho mỗi lần chạy để tránh xung đột file
            _currentOutputDir = Path.Combine(OUTPUT_DIR, Guid.NewGuid().ToString());
            Directory.CreateDirectory(_currentOutputDir);
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            // Dọn dẹp thư mục của lần chạy hiện tại
            if (Directory.Exists(_currentOutputDir))
            {
                Directory.Delete(_currentOutputDir, true);
            }
        }

        // =====================================================================
        // BENCHMARK 1: ENCODE TO HLS (h264_nvenc + aac)
        // =====================================================================

        //[Benchmark(Description = "Process: Encode to HLS")]
        //public void Process_EncodeToHls()
        //{
        //    string arguments = $"-y -i {VideoPath} -c:v h264_nvenc -c:a aac -hls_time 4 -hls_list_size 0 -hls_segment_filename \"{_currentOutputDir}/segment_%03d.ts\" \"{_currentOutputDir}/playlist.m3u8\"";
        //    RunProcess(arguments);
        //}

        //[Benchmark(Description = "AutoGen: Encode to HLS")]
        //public void AutoGen_EncodeToHls()
        //{
        //    // Đây là phiên bản phức tạp, kết hợp cả decode và encode
        //    // Do độ dài và phức tạp, ta sẽ đặt logic vào một hàm riêng
        //    // Trong một kịch bản thực tế, bạn sẽ cần triển khai logic transcoding đầy đủ ở đây
        //    // Để benchmark này chạy được, bạn cần một hàm TranscodeToHls đã được triển khai.
        //    // Vì nó quá dài, tôi sẽ để trống để minh họa. Nếu cần, tôi sẽ cung cấp code đầy đủ.
        //    // TranscodeToHls(VideoPath, Path.Combine(_currentOutputDir, "playlist.m3u8"));

        //    // Lưu ý: Triển khai AutoGen cho việc transcoding rất phức tạp.
        //    // Để giữ ví dụ này gọn gàng, ta sẽ tập trung vào phần remuxing bên dưới
        //    // vốn đã thể hiện rõ sự khác biệt về hiệu năng.
        //}


        // =====================================================================
        // BENCHMARK 2: REMUX TO HLS (-c copy)
        // =====================================================================

        [Benchmark(Description = "Call process (no encode)")]
        public void Process_RemuxToHls()
        {
            string arguments = $"-y -i \"{VideoPath}\" -c copy -hls_time 4 -hls_list_size 0 -hls_segment_filename \"{_currentOutputDir}/segment_%05d.ts\" \"{_currentOutputDir}/playlist.m3u8\"";
            RunProcess(arguments);
        }

        [Benchmark(Description = "Call native (no encode")]
        public void AutoGen_RemuxToHls()
        {
            var inputFile = VideoPath;
            var outputFile = Path.Combine(_currentOutputDir, "playlist.m3u8");

            AVFormatContext* pInputFormatContext = null;
            AVFormatContext* pOutputFormatContext = null;
            AVDictionary* hlsOpts = null;

            try
            {
                if ((ffmpeg.avformat_open_input(&pInputFormatContext, inputFile, null, null)) < 0) throw new Exception("Open input failed.");
                if ((ffmpeg.avformat_find_stream_info(pInputFormatContext, null)) < 0) throw new Exception("Find stream info failed.");

                ffmpeg.avformat_alloc_output_context2(&pOutputFormatContext, null, "hls", outputFile);
                if (pOutputFormatContext == null) throw new Exception("Alloc output context failed.");

                for (int i = 0; i < pInputFormatContext->nb_streams; i++)
                {
                    AVStream* inStream = pInputFormatContext->streams[i];
                    AVStream* outStream = ffmpeg.avformat_new_stream(pOutputFormatContext, null);
                    if (outStream == null) throw new Exception("New stream failed.");
                    ffmpeg.avcodec_parameters_copy(outStream->codecpar, inStream->codecpar);
                    outStream->codecpar->codec_tag = 0;
                }

                if ((pOutputFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                {
                    if ((ffmpeg.avio_open(&pOutputFormatContext->pb, outputFile, ffmpeg.AVIO_FLAG_WRITE)) < 0) throw new Exception("Open output file failed.");
                }

                // Thiết lập các tùy chọn HLS
                var segmentPath = Path.Combine(_currentOutputDir, "segment_%05d.ts").Replace('\\', '/');
                ffmpeg.av_dict_set(&hlsOpts, "hls_time", "4", 0);
                ffmpeg.av_dict_set(&hlsOpts, "hls_list_size", "0", 0);
                ffmpeg.av_dict_set(&hlsOpts, "hls_segment_filename", segmentPath, 0);

                if ((ffmpeg.avformat_write_header(pOutputFormatContext, &hlsOpts)) < 0) throw new Exception("Write header failed.");

                var pPacket = ffmpeg.av_packet_alloc();
                while (ffmpeg.av_read_frame(pInputFormatContext, pPacket) >= 0)
                {
                    AVStream* inStream = pInputFormatContext->streams[pPacket->stream_index];
                    AVStream* outStream = pOutputFormatContext->streams[pPacket->stream_index];
                    ffmpeg.av_packet_rescale_ts(pPacket, inStream->time_base, outStream->time_base);
                    pPacket->pos = -1;
                    ffmpeg.av_interleaved_write_frame(pOutputFormatContext, pPacket);
                    ffmpeg.av_packet_unref(pPacket);
                }
                ffmpeg.av_packet_free(&pPacket);

                ffmpeg.av_write_trailer(pOutputFormatContext);
            }
            finally
            {
                // Dọn dẹp
                ffmpeg.av_dict_free(&hlsOpts);
                if (pInputFormatContext != null) ffmpeg.avformat_close_input(&pInputFormatContext);
                if (pOutputFormatContext != null)
                {
                    if ((pOutputFormatContext->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                        ffmpeg.avio_closep(&pOutputFormatContext->pb);
                    ffmpeg.avformat_free_context(pOutputFormatContext);
                }
            }
        }

        [Benchmark(Description = "Call process (h264_nvenc + aac)")]
        public void Process_RemuxToHls_Encode()
        {
            string arguments = $"-y -i \"{VideoPath}\" -c:v h264_nvenc -c:a aac -hls_time 4 -hls_list_size 0 -hls_segment_filename \"{_currentOutputDir}/segment_%05d.ts\" \"{_currentOutputDir}/playlist.m3u8\"";
            RunProcess(arguments);
        }

        [Benchmark(Description = "Call native (h264_nvenc + aac)")]
        public void AutoGen_EncodeToHls()
        {
            static void Throw(int err, string where)
            {
                if (err < 0)
                {
                    byte* buf = stackalloc byte[1024];
                    ffmpeg.av_strerror(err, buf, 1024UL);
                    string msg = System.Runtime.InteropServices.Marshal.PtrToStringAnsi((IntPtr)buf) ?? "ffmpeg error";
                    throw new Exception($"{where}: {msg}");
                }
            }

            string inputFile = VideoPath;
            string outputFile = Path.Combine(_currentOutputDir, "playlist.m3u8");
            string segPath = Path.Combine(_currentOutputDir, "segment_%05d.ts").Replace('\\', '/');
            int hlsTime = 4;

            AVFormatContext* ifmt = null, ofmt = null;
            AVCodecContext* vdec = null, adec = null, venc = null, aenc = null;
            AVStream* vOut, aOut = null;
            SwsContext* sws = null;
            SwrContext* swr = null;
            AVAudioFifo* afifo = null;
            AVFrame* yuv = null;
            AVDictionary* hlsOpts = null;

            bool needScale = false;
            bool audioCopy = false;
            int vIndex = -1, aIndex = -1;

            long aSamplesWritten = 0;
            long vPktDur = 0;

            // pre-allocated plane pointer arrays (tránh stackalloc trong loop)
            byte** planesIn = null;
            byte** planesOut = null;

            try
            {
                // ---------- Input ----------
                Throw(ffmpeg.avformat_open_input(&ifmt, inputFile, null, null), "open_input");
                Throw(ffmpeg.avformat_find_stream_info(ifmt, null), "find_stream_info");

                vIndex = ffmpeg.av_find_best_stream(ifmt, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, null, 0);
                if (vIndex < 0) throw new Exception("No video stream.");
                aIndex = ffmpeg.av_find_best_stream(ifmt, AVMediaType.AVMEDIA_TYPE_AUDIO, -1, -1, null, 0);

                AVStream* vInSt = ifmt->streams[vIndex];
                AVStream* aInSt = aIndex >= 0 ? ifmt->streams[aIndex] : null;

                // video decoder (threading)
                {
                    AVCodec* dec = ffmpeg.avcodec_find_decoder(vInSt->codecpar->codec_id);
                    vdec = ffmpeg.avcodec_alloc_context3(dec);
                    Throw(ffmpeg.avcodec_parameters_to_context(vdec, vInSt->codecpar), "video params->ctx");
                    vdec->thread_type = ffmpeg.FF_THREAD_FRAME;
                    vdec->thread_count = 0; // auto
                    Throw(ffmpeg.avcodec_open2(vdec, dec, null), "open vdec");
                }

                // audio decoder
                if (aIndex >= 0)
                {
                    AVCodec* dec = ffmpeg.avcodec_find_decoder(aInSt->codecpar->codec_id);
                    adec = ffmpeg.avcodec_alloc_context3(dec);
                    Throw(ffmpeg.avcodec_parameters_to_context(adec, aInSt->codecpar), "audio params->ctx");
                    Throw(ffmpeg.avcodec_open2(adec, dec, null), "open adec");
                }

                // ---------- Output ----------
                Throw(ffmpeg.avformat_alloc_output_context2(&ofmt, null, "hls", outputFile), "alloc_output");
                if (ofmt == null) throw new Exception("alloc_output failed.");

                ffmpeg.av_dict_set(&hlsOpts, "hls_time", hlsTime.ToString(), 0);
                ffmpeg.av_dict_set(&hlsOpts, "hls_list_size", "0", 0);
                ffmpeg.av_dict_set(&hlsOpts, "hls_flags", "independent_segments", 0);
                ffmpeg.av_dict_set(&hlsOpts, "hls_segment_type", "mpegts", 0);
                ffmpeg.av_dict_set(&hlsOpts, "hls_segment_filename", segPath, 0);

                // --- video encoder: h264_nvenc (fast path) ---
                {
                    AVCodec* enc = ffmpeg.avcodec_find_encoder_by_name("h264_nvenc");
                    if (enc == null) throw new Exception("h264_nvenc not found.");

                    vOut = ffmpeg.avformat_new_stream(ofmt, enc);
                    venc = ffmpeg.avcodec_alloc_context3(enc);

                    AVRational inFps = vInSt->avg_frame_rate.num != 0 ? vInSt->avg_frame_rate : new AVRational { num = 30, den = 1 };
                    double fpsd = ffmpeg.av_q2d(inFps);
                    int fps = (int)Math.Round(fpsd <= 0 ? 30.0 : fpsd);
                    int gop = Math.Max(1, fps * hlsTime);

                    AVPixelFormat srcPix = vdec->pix_fmt;
                    bool srcOk = srcPix == AVPixelFormat.AV_PIX_FMT_YUV420P
                              || srcPix == AVPixelFormat.AV_PIX_FMT_NV12
                              || srcPix == AVPixelFormat.AV_PIX_FMT_P010LE;

                    venc->width = vdec->width;
                    venc->height = vdec->height;
                    venc->pix_fmt = srcOk ? srcPix : AVPixelFormat.AV_PIX_FMT_NV12;
                    venc->time_base = new AVRational { num = inFps.den, den = inFps.num };
                    venc->framerate = inFps;

                    // speed profile
                    ffmpeg.av_opt_set(venc->priv_data, "rc", "constqp", 0);
                    ffmpeg.av_opt_set_int(venc->priv_data, "qp", 23, 0);
                    ffmpeg.av_opt_set(venc->priv_data, "preset", "p2", 0);
                    ffmpeg.av_opt_set_int(venc->priv_data, "rc-lookahead", 0, 0);
                    ffmpeg.av_opt_set_int(venc->priv_data, "g", gop, 0);
                    ffmpeg.av_opt_set_int(venc->priv_data, "keyint_min", gop, 0);
                    ffmpeg.av_opt_set_int(venc->priv_data, "forced-idr", 1, 0);

                    Throw(ffmpeg.avcodec_open2(venc, enc, null), "open nvenc");
                    Throw(ffmpeg.avcodec_parameters_from_context(vOut->codecpar, venc), "video params->stream");
                    vOut->time_base = venc->time_base;

                    // duration 1 frame theo encoder tb (để gán nếu 0)
                    {
                        int fpsDen = (int)Math.Round(ffmpeg.av_q2d(venc->framerate));
                        if (fpsDen <= 0) fpsDen = 30;
                        AVRational oneFps = new AVRational { num = 1, den = fpsDen };
                        vPktDur = ffmpeg.av_rescale_q(1, oneFps, venc->time_base);
                    }

                    needScale = !(srcOk && vdec->width == venc->width && vdec->height == venc->height);
                    if (needScale)
                    {
                        sws = ffmpeg.sws_getContext(
                            vdec->width, vdec->height, vdec->pix_fmt,
                            venc->width, venc->height, venc->pix_fmt,
                            (int)SwsFlags.SWS_BILINEAR, null, null, null);

                        yuv = ffmpeg.av_frame_alloc();
                        yuv->format = (int)venc->pix_fmt;
                        yuv->width = venc->width;
                        yuv->height = venc->height;
                        Throw(ffmpeg.av_frame_get_buffer(yuv, 32), "frame_get_buffer");
                    }
                }

                // --- audio: copy AAC, else encode AAC + FIFO ---
                if (aIndex >= 0)
                {
                    if (aInSt->codecpar->codec_id == AVCodecID.AV_CODEC_ID_AAC)
                    {
                        audioCopy = true;
                        aOut = ffmpeg.avformat_new_stream(ofmt, null);
                        Throw(ffmpeg.avcodec_parameters_copy(aOut->codecpar, aInSt->codecpar), "aac copy params");
                        aOut->codecpar->codec_tag = 0;
                        aOut->time_base = aInSt->time_base;
                    }
                    else
                    {
                        AVCodec* aac = ffmpeg.avcodec_find_encoder_by_name("aac");
                        if (aac == null) throw new Exception("aac encoder not found.");
                        aOut = ffmpeg.avformat_new_stream(ofmt, aac);
                        aenc = ffmpeg.avcodec_alloc_context3(aac);

                        aenc->sample_rate = adec->sample_rate;
                        aenc->ch_layout = adec->ch_layout;
                        aenc->sample_fmt = aac->sample_fmts[0]; // thường FLTP
                        aenc->bit_rate = 128_000;
                        aenc->time_base = new AVRational { num = 1, den = aenc->sample_rate };
                        aOut->time_base = aenc->time_base;

                        Throw(ffmpeg.avcodec_open2(aenc, aac, null), "open aac");
                        Throw(ffmpeg.avcodec_parameters_from_context(aOut->codecpar, aenc), "audio params->stream");

                        int aFrameSize = aenc->frame_size > 0 ? aenc->frame_size : 1024;
                        afifo = ffmpeg.av_audio_fifo_alloc(aenc->sample_fmt, aenc->ch_layout.nb_channels, aFrameSize * 8);
                        if (afifo == null) throw new Exception("av_audio_fifo_alloc failed");

                        if (adec->sample_fmt != aenc->sample_fmt)
                        {
                            AVChannelLayout inLayout = adec->ch_layout;
                            AVChannelLayout outLayout = aenc->ch_layout;
                            Throw(ffmpeg.swr_alloc_set_opts2(&swr,
                                &outLayout, aenc->sample_fmt, aenc->sample_rate,
                                &inLayout, adec->sample_fmt, adec->sample_rate,
                                0, null), "swr_alloc_set_opts2");
                            Throw(ffmpeg.swr_init(swr), "swr_init");
                        }
                    }
                }

                // open io + header
                if ((ofmt->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0)
                    Throw(ffmpeg.avio_open(&ofmt->pb, outputFile, ffmpeg.AVIO_FLAG_WRITE), "avio_open");
                Throw(ffmpeg.avformat_write_header(ofmt, &hlsOpts), "write_header");
                ffmpeg.av_dict_free(&hlsOpts);

                // pre-alloc plane arrays (byte**)
                planesIn = (byte**)ffmpeg.av_calloc(8, (ulong)sizeof(byte*));
                planesOut = (byte**)ffmpeg.av_calloc(8, (ulong)sizeof(byte*));
                if (planesIn == null || planesOut == null) throw new Exception("av_calloc planes failed");

                // loop
                AVPacket* ipkt = ffmpeg.av_packet_alloc();
                AVPacket* voutPkt = ffmpeg.av_packet_alloc();
                AVPacket* aoutPkt = ffmpeg.av_packet_alloc();
                AVFrame* vfrm = ffmpeg.av_frame_alloc();
                AVFrame* afrm = ffmpeg.av_frame_alloc();

                while (ffmpeg.av_read_frame(ifmt, ipkt) >= 0)
                {
                    // audio copy
                    if (audioCopy && ipkt->stream_index == aIndex)
                    {
                        ffmpeg.av_packet_rescale_ts(ipkt, aInSt->time_base, aOut->time_base);
                        ipkt->stream_index = aOut->index;
                        ipkt->pos = -1;
                        Throw(ffmpeg.av_interleaved_write_frame(ofmt, ipkt), "write audio copy");
                        ffmpeg.av_packet_unref(ipkt);
                        continue;
                    }

                    // video
                    if (ipkt->stream_index == vIndex)
                    {
                        Throw(ffmpeg.avcodec_send_packet(vdec, ipkt), "send v pkt");
                        ffmpeg.av_packet_unref(ipkt);

                        while (true)
                        {
                            int r = ffmpeg.avcodec_receive_frame(vdec, vfrm);
                            if (r == ffmpeg.AVERROR(ffmpeg.EAGAIN) || r == ffmpeg.AVERROR_EOF) break;
                            Throw(r, "recv v frame");

                            AVFrame* toSend = vfrm;

                            if (needScale)
                            {
                                ffmpeg.sws_scale(sws, vfrm->data, vfrm->linesize, 0, vdec->height, yuv->data, yuv->linesize);
                                yuv->pts = ffmpeg.av_rescale_q(vfrm->pts, vInSt->time_base, venc->time_base);
                                toSend = yuv;
                            }
                            else
                            {
                                vfrm->pts = ffmpeg.av_rescale_q(vfrm->pts, vInSt->time_base, venc->time_base);
                            }

                            Throw(ffmpeg.avcodec_send_frame(venc, toSend), "send v frame enc");
                            while (ffmpeg.avcodec_receive_packet(venc, voutPkt) == 0)
                            {
                                if (voutPkt->duration == 0) voutPkt->duration = (int)vPktDur;
                                ffmpeg.av_packet_rescale_ts(voutPkt, venc->time_base, vOut->time_base);
                                voutPkt->stream_index = vOut->index;
                                Throw(ffmpeg.av_interleaved_write_frame(ofmt, voutPkt), "write v pkt");
                                ffmpeg.av_packet_unref(voutPkt);
                            }
                            ffmpeg.av_frame_unref(vfrm);
                        }
                        continue;
                    }

                    // audio encode
                    if (!audioCopy && aIndex >= 0 && ipkt->stream_index == aIndex)
                    {
                        Throw(ffmpeg.avcodec_send_packet(adec, ipkt), "send a pkt");
                        ffmpeg.av_packet_unref(ipkt);

                        while (true)
                        {
                            int r = ffmpeg.avcodec_receive_frame(adec, afrm);
                            if (r == ffmpeg.AVERROR(ffmpeg.EAGAIN) || r == ffmpeg.AVERROR_EOF) break;
                            Throw(r, "recv a frame");

                            AVFrame* conv = afrm; AVFrame* tmp = null;

                            bool needSwrNow =
                                swr != null ||
                                afrm->format != (int)aenc->sample_fmt ||
                                afrm->sample_rate != aenc->sample_rate ||
                                afrm->ch_layout.nb_channels != aenc->ch_layout.nb_channels;

                            if (needSwrNow)
                            {
                                tmp = ffmpeg.av_frame_alloc();
                                tmp->format = (int)aenc->sample_fmt;
                                tmp->sample_rate = aenc->sample_rate;
                                tmp->ch_layout = aenc->ch_layout;
                                tmp->nb_samples = afrm->nb_samples;
                                Throw(ffmpeg.av_frame_get_buffer(tmp, 0), "a tmp buffer");
                                Throw(ffmpeg.swr_convert_frame(swr, tmp, afrm), "swr_convert_frame");
                                conv = tmp;
                            }

                            // FIFO write (realloc đủ lớn)
                            int need = ffmpeg.av_audio_fifo_size(afifo) + conv->nb_samples;
                            Throw(ffmpeg.av_audio_fifo_realloc(afifo, need), "av_audio_fifo_realloc");

                            for (uint i = 0; i < 8; i++) planesIn[i] = conv->data[i];
                            int written = ffmpeg.av_audio_fifo_write(afifo, (void**)planesIn, conv->nb_samples);
                            if (written < conv->nb_samples) throw new Exception("av_audio_fifo_write short");

                            if (tmp != null) ffmpeg.av_frame_free(&tmp);
                            ffmpeg.av_frame_unref(afrm);

                            // drain theo frame_size
                            int frameSize = aenc->frame_size > 0 ? aenc->frame_size : 1024;
                            while (ffmpeg.av_audio_fifo_size(afifo) >= frameSize)
                            {
                                AVFrame* aOutFrame = ffmpeg.av_frame_alloc();
                                aOutFrame->format = (int)aenc->sample_fmt;
                                aOutFrame->sample_rate = aenc->sample_rate;
                                aOutFrame->ch_layout = aenc->ch_layout;
                                aOutFrame->nb_samples = frameSize;
                                Throw(ffmpeg.av_frame_get_buffer(aOutFrame, 0), "a out buffer");

                                for (uint i = 0; i < 8; i++) planesOut[i] = aOutFrame->data[i];
                                int rd = ffmpeg.av_audio_fifo_read(afifo, (void**)planesOut, aOutFrame->nb_samples);
                                if (rd != aOutFrame->nb_samples) throw new Exception("av_audio_fifo_read short");

                                aOutFrame->pts = ffmpeg.av_rescale_q(aSamplesWritten,
                                    new AVRational { num = 1, den = aenc->sample_rate }, aenc->time_base);
                                aSamplesWritten += aOutFrame->nb_samples;

                                Throw(ffmpeg.avcodec_send_frame(aenc, aOutFrame), "send a frame enc");
                                while (ffmpeg.avcodec_receive_packet(aenc, aoutPkt) == 0)
                                {
                                    ffmpeg.av_packet_rescale_ts(aoutPkt, aenc->time_base, aOut->time_base);
                                    aoutPkt->stream_index = aOut->index;
                                    Throw(ffmpeg.av_interleaved_write_frame(ofmt, aoutPkt), "write a pkt");
                                    ffmpeg.av_packet_unref(aoutPkt);
                                }
                                ffmpeg.av_frame_free(&aOutFrame);
                            }
                        }
                    }
                    else
                    {
                        ffmpeg.av_packet_unref(ipkt);
                    }
                }

                // ---------- flush video ----------
                Throw(ffmpeg.avcodec_send_frame(venc, null), "flush v");
                {
                    AVPacket* vp = ffmpeg.av_packet_alloc();
                    while (ffmpeg.avcodec_receive_packet(venc, vp) == 0)
                    {
                        if (vp->duration == 0) vp->duration = 1;
                        ffmpeg.av_packet_rescale_ts(vp, venc->time_base, vOut->time_base);
                        vp->stream_index = vOut->index;
                        Throw(ffmpeg.av_interleaved_write_frame(ofmt, vp), "write v flush");
                        ffmpeg.av_packet_unref(vp);
                    }
                    ffmpeg.av_packet_free(&vp);
                }

                // ---------- flush audio ----------
                if (!audioCopy && aenc != null)
                {
                    int frameSize = aenc->frame_size > 0 ? aenc->frame_size : 1024;
                    int remain = afifo != null ? ffmpeg.av_audio_fifo_size(afifo) : 0;

                    while (remain > 0)
                    {
                        int toSend = Math.Min(remain, frameSize);

                        AVFrame* aOutFrame = ffmpeg.av_frame_alloc();
                        aOutFrame->format = (int)aenc->sample_fmt;
                        aOutFrame->sample_rate = aenc->sample_rate;
                        aOutFrame->ch_layout = aenc->ch_layout;
                        aOutFrame->nb_samples = frameSize;
                        Throw(ffmpeg.av_frame_get_buffer(aOutFrame, 0), "flush a out buffer");

                        for (uint i = 0; i < 8; i++) planesOut[i] = aOutFrame->data[i];
                        int actually = ffmpeg.av_audio_fifo_read(afifo, (void**)planesOut, toSend);

                        if (actually < aOutFrame->nb_samples)
                        {
                            Throw(ffmpeg.av_samples_set_silence(planesOut, actually,
                                aOutFrame->nb_samples - actually, aOutFrame->ch_layout.nb_channels, aenc->sample_fmt),
                                "av_samples_set_silence");
                        }

                        aOutFrame->pts = ffmpeg.av_rescale_q(aSamplesWritten,
                            new AVRational { num = 1, den = aenc->sample_rate }, aenc->time_base);
                        aSamplesWritten += aOutFrame->nb_samples;

                        Throw(ffmpeg.avcodec_send_frame(aenc, aOutFrame), "send a frame enc (flush)");
                        while (ffmpeg.avcodec_receive_packet(aenc, aoutPkt) == 0)
                        {
                            ffmpeg.av_packet_rescale_ts(aoutPkt, aenc->time_base, aOut->time_base);
                            aoutPkt->stream_index = aOut->index;
                            Throw(ffmpeg.av_interleaved_write_frame(ofmt, aoutPkt), "write a flush pkt");
                            ffmpeg.av_packet_unref(aoutPkt);
                        }
                        ffmpeg.av_frame_free(&aOutFrame);

                        remain = ffmpeg.av_audio_fifo_size(afifo);
                    }

                    Throw(ffmpeg.avcodec_send_frame(aenc, null), "flush a");
                    while (ffmpeg.avcodec_receive_packet(aenc, aoutPkt) == 0)
                    {
                        ffmpeg.av_packet_rescale_ts(aoutPkt, aenc->time_base, aOut->time_base);
                        aoutPkt->stream_index = aOut->index;
                        Throw(ffmpeg.av_interleaved_write_frame(ofmt, aoutPkt), "write a flush tail");
                        ffmpeg.av_packet_unref(aoutPkt);
                    }
                }

                Throw(ffmpeg.av_write_trailer(ofmt), "write_trailer");

                // free temps
                ffmpeg.av_frame_free(&vfrm);
                ffmpeg.av_frame_free(&afrm);
                ffmpeg.av_packet_free(&ipkt);
                ffmpeg.av_packet_free(&voutPkt);
                ffmpeg.av_packet_free(&aoutPkt);
            }
            finally
            {
                if (planesIn != null) ffmpeg.av_free(planesIn);
                if (planesOut != null) ffmpeg.av_free(planesOut);

                if (afifo != null) ffmpeg.av_audio_fifo_free(afifo);

                if (sws != null) ffmpeg.sws_freeContext(sws);
                if (swr != null) ffmpeg.swr_free(&swr);
                if (yuv != null) ffmpeg.av_frame_free(&yuv);

                if (venc != null) ffmpeg.avcodec_free_context(&venc);
                if (aenc != null) ffmpeg.avcodec_free_context(&aenc);
                if (vdec != null) ffmpeg.avcodec_free_context(&vdec);
                if (adec != null) ffmpeg.avcodec_free_context(&adec);

                if (ifmt != null) ffmpeg.avformat_close_input(&ifmt);
                if (ofmt != null)
                {
                    if ((ofmt->oformat->flags & ffmpeg.AVFMT_NOFILE) == 0) ffmpeg.avio_closep(&ofmt->pb);
                    ffmpeg.avformat_free_context(ofmt);
                }
                ffmpeg.av_dict_free(&hlsOpts);
            }
        }




        private void RunProcess(string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = arguments,
                    FileName = "ffmpeg.exe",
                    UseShellExecute = false,      // Đặt thành false để kiểm soát trực tiếp
                    CreateNoWindow = true,        // Chạy ẩn, không hiện cửa sổ console
                    RedirectStandardOutput = true, // Bắt luồng output
                    RedirectStandardError = true,  // Bắt luồng lỗi
                }
            };

            process.Start();

            // Đọc output/error (tùy chọn nhưng tốt để debug)
            string error = process.StandardError.ReadToEnd();
            // string output = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                // Bây giờ biến 'error' đã có nội dung
                throw new Exception($"FFmpeg process failed with exit code {process.ExitCode}: {error}");
            }
        }
    }
}