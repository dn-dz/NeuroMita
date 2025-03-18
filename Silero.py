from telethon import TelegramClient, events
import os
import sys
import time
import random
import pygame
import asyncio
import emoji
from telethon.tl.types import MessageMediaDocument, DocumentAttributeAudio

import tkinter as tk
import platform

from AudioConverter import AudioConverter
from utils import SH


# Пример использования:
class TelegramBotHandler:

    def __init__(
        self, gui, api_id, api_hash, phone, tg_bot, message_limit_per_minute=20
    ):
        # Получение параметров из окружения
        self.api_id = api_id
        self.api_hash = api_hash
        self.phone = phone
        self.tg_bot = tg_bot  # Юзернейм Silero бота
        self.gui = gui
        self.patch_to_sound_file = ""
        self.last_speaker_command = ""

        #Считываем значение настройки SILERO_TIME_LIMIT
        self.silero_time_limit = int(gui.settings.get("SILERO_TIME", "10"))

        #Проверяем значение. Если пусто подставляем по умолчанию 10 секунд.
        if not hasattr(self, "silero_time_limit") or self.silero_time_limit is None:
            self.silero_time_limit = 10

        if getattr(sys, "frozen", False):
            # Если программа собрана в exe, получаем путь к исполняемому файлу
            base_dir = os.path.dirname(sys.executable)

            # Альтернативный вариант: если ffmpeg всё же упакован в _MEIPASS
            alt_base_dir = sys._MEIPASS
        else:
            # Если программа запускается как скрипт
            base_dir = os.path.dirname(__file__)
            alt_base_dir = base_dir  # Для единообразия

        # Проверяем, где лежит ffmpeg
        ffmpeg_rel_path = os.path.join(
            "ffmpeg-7.1-essentials_build", "bin", "ffmpeg.exe"
        )

        ffmpeg_path = os.path.join(base_dir, ffmpeg_rel_path)
        if not os.path.exists(ffmpeg_path):
            # Если не нашли в base_dir, пробуем _MEIPASS (актуально для PyInstaller)
            ffmpeg_path = os.path.join(alt_base_dir, ffmpeg_rel_path)

        self.ffmpeg_path = ffmpeg_path

        # Системные параметры
        device_model = platform.node()  # Имя устройства
        system_version = f"{platform.system()} {platform.release()}"  # ОС и версия
        app_version = "1.0.0"  # Версия приложения задается вручную

        # Параметры бота
        self.message_limit_per_minute = message_limit_per_minute
        self.message_count = 0
        self.start_time = time.time()

        self.client = None
        try:
            # Создание клиента Telegram с системными параметрами
            self.client = TelegramClient(
                "session_name",
                int(self.api_id),
                self.api_hash,
                device_model=device_model,
                system_version=system_version,
                app_version=app_version,
            )
        except:
            print("Проблема в ините тг")
            print(SH(self.api_id))
            print(SH(self.api_hash))

    def reset_message_count(self):
        """Сбрасывает счетчик сообщений каждую минуту."""
        if time.time() - self.start_time > 60:
            self.message_count = 0
            self.start_time = time.time()

    async def play_audio(self, file_path):
        """Проигрывает аудиофайл (MP3/OGG)."""

        def play():
            pygame.mixer.init()
            pygame.mixer.music.load(file_path)  # Pygame поддерживает MP3 и OGG
            pygame.mixer.music.play()
            while pygame.mixer.music.get_busy():  # Ждем завершения воспроизведения
                pygame.time.Clock().tick(10)
            pygame.mixer.music.stop()
            pygame.mixer.quit()

        await asyncio.to_thread(play)  # Запуск в отдельном потоке

    async def handle_voice_file(self, file_path):
        """Проигрывает звуковой файл (MP3 или OGG)."""
        try:
            print(f"Проигрываю файл: {file_path}")
            await self.play_audio(file_path)
            if os.path.exists(file_path):
                try:
                    await asyncio.sleep(0.02)
                    os.remove(file_path)
                    print(f"Файл {file_path} удалён.")
                except Exception as e:
                    print(f"Файл {file_path} НЕ удалён. Ошибка: {e}")
        except Exception as e:
            print(f"Ошибка при воспроизведении файла: {e}")

    async def send_and_receive(self, input_message, speaker_command):
        """Отправляет сообщение боту и обрабатывает ответ."""
        global message_count

        if not input_message or not speaker_command:
            return

        #При компиляции нужно добавить в строку компилятора --add-data "%USERPROFILE%\AppData\Local\Programs\Python\Python313\Lib\site-packages\emoji\unicode_codes\emoji.json;emoji\unicode_codes"
        #Удаляем эмодзи из озвучки:
        input_message = emoji.replace_emoji(input_message)  # Заменяет все эмодзи на пустую строку

        if self.last_speaker_command != speaker_command:
            await self.client.send_message(self.tg_bot, speaker_command)
            self.last_speaker_command = speaker_command
            await asyncio.sleep(0.45)

            if self.gui.silero_turn_off_video:
                await self.client.send_message(self.tg_bot, "/videonotes")

                await asyncio.sleep(0.45)

                # Получаем последнее сообщение от бота
                messages = await self.client.get_messages(self.tg_bot, limit=1)
                last_message = messages[0] if messages else None

                # Проверяем содержимое последнего сообщения
                if last_message and last_message.text == "Кружки включены!":
                    # Если условие выполнено, отправляем команду еще раз
                    await self.client.send_message(self.tg_bot, "/videonotes")

        self.last_speaker_command = speaker_command

        self.reset_message_count()

        if self.message_count >= self.message_limit_per_minute:
            print("Превышен лимит сообщений. Ожидаем...")
            await asyncio.sleep(random.uniform(10, 15))
            return

        # Отправка сообщения боту
        if self.tg_bot == "@CrazyMitaAIbot":
            input_message = f"/voice {input_message}"
        await self.client.send_message(self.tg_bot, input_message)
        self.message_count += 1

        # Ожидание ответа от бота
        print("Ожидание ответа от бота...")
        response = None
        attempts = 0
        attempts_per_second = 2

        attempts_max = self.silero_time_limit * attempts_per_second

        await asyncio.sleep(0.2)
        while attempts <= attempts_max:  # Попытки получения ответа
            exists = await self.is_reply_message_received(requestChatID, requestID)
            print("Сообщение найдено!" if exists else "Сообщение не найдено.")
            async for message in self.client.iter_messages(self.tg_bot, limit=1):
                if message.media and isinstance(message.media, MessageMediaDocument):
                    doc = message.media.document

                    # Проверяем MP3-файл
                    if "audio/mpeg" in doc.mime_type:
                        response = message
                        break

                    # Проверяем голосовое сообщение (OGG, voice)
                    if "audio/ogg" in doc.mime_type:
                        for attr in doc.attributes:
                            if isinstance(attr, DocumentAttributeAudio) and attr.voice:
                                response = message
                                break
            if response:  # Если ответ найден, выходим из цикла
                break
            print(f"Попытка {attempts + 1}/{attempts_max}. Ответ от бота не найден.")
            attempts += 1
            await asyncio.sleep(1 / attempts_per_second)  # Немного подождем

        if not response:
            print(f"Ответ от бота не получен после {attempts_max} попыток.")
            return

        print("Ответ получен")
        # Обработка полученного сообщения
        if response.media and isinstance(response.media, MessageMediaDocument):
            if response and response.media:
                print(f"DEBUG. Ответ от бота: {response}")
                file_path = await self.client.download_media(response.media)

                print(f"Файл загружен: {file_path}")
                sound_absolute_path = os.path.abspath(file_path)
                if self.gui.ConnectedToGame:
                    print("Подключен к игре, нужна конвертация")

                    # Генерируем путь для WAV-файла на основе имени исходного MP3
                    base_name = os.path.splitext(os.path.basename(file_path))[
                        0
                    ]  # Получаем имя файла без расширения
                    wav_path = os.path.join(
                        os.path.dirname(file_path), f"{base_name}.wav"
                    )  # Создаем новый путь

                    # Получаем абсолютный путь

                    absolute_wav_path = os.path.abspath(wav_path)
                    # Конвертируем MP3 в WAV
                    # await  AudioConverter.convert_mp3_to_wav(sound_absolute_path, absolute_wav_path)
                    await AudioConverter.convert_to_wav(
                        sound_absolute_path, absolute_wav_path
                    )

                    try:
                        print(f"Удаляю файл: {sound_absolute_path}")
                        os.remove(sound_absolute_path)
                        print(f"Файл {sound_absolute_path} удалён.")
                    except OSError as remove_error:
                        print(
                            f"Ошибка при удалении файла {sound_absolute_path}: {remove_error}"
                        )

                    # .BnmRvcModel.process(absolute_wav_path, absolute_wav_path+"_RVC_.wav")

                    self.gui.patch_to_sound_file = absolute_wav_path
                    print(f"Файл wav загружен: {absolute_wav_path}")
                else:
                    print(f"Отправлен воспроизводится: {sound_absolute_path}")
                    await self.handle_voice_file(file_path)
        elif response.text:  # Если сообщение текстовое
            print(f"Ответ от бота: {response.text}")

    async def start(self):
        print("Запуск коннектора ТГ!")
        try:
            await self.client.connect()

            # Проверяем, авторизован ли уже клиент
            if not await self.client.is_user_authorized():
                # Создаем окно для ввода кода подтверждения
                code_window = tk.Toplevel()
                code_window.title("Подтверждение Telegram")
                code_window.geometry("300x150")
                code_window.resizable(False, False)

                frame = tk.Frame(code_window)
                frame.place(relx=0.5, rely=0.5, anchor="center")

                code_var = tk.StringVar()

                code_entry = tk.Entry(
                    frame, textvariable=code_var, width=20, justify="center"
                )
                code_entry.pack(pady=10)
                code_entry.focus()  # Устанавливаем фокус на поле ввода

                code_future = asyncio.Future()

                def submit_code():
                    if code_var.get().strip():  # Проверяем, что код не пустой
                        code_future.set_result(code_var.get().strip())
                        code_window.destroy()
                    else:
                        tk.messagebox.showerror("Ошибка", "Введите код подтверждения")

                def on_enter(event):
                    submit_code()

                code_entry.bind("<Return>", on_enter)  # Добавляем обработку Enter

                submit_button = tk.Button(
                    frame,
                    text="Подтвердить",
                    command=submit_code,
                    width=15,
                    relief="flat",
                )
                submit_button.pack(pady=15)

                # Ждем код подтверждения
                try:
                    await self.client.sign_in(phone=self.phone)
                    verification_code = await code_future
                    await self.client.sign_in(phone=self.phone, code=verification_code)
                except Exception as e:
                    print(f"Ошибка при вводе кода: {e}")
                    raise

            await self.client.send_message(self.tg_bot, "/start")
            self.gui.silero_connected.set(True)

            if self.tg_bot == "@silero_voice_bot":
                await asyncio.sleep(0.35)
                await self.client.send_message(self.tg_bot, "/speaker mita")
                self.last_speaker_command = "/speaker mita"
                await asyncio.sleep(0.35)
                await self.client.send_message(self.tg_bot, "/mp3")
                await asyncio.sleep(0.35)
                await self.client.send_message(self.tg_bot, "/hd")
                await asyncio.sleep(0.35)
                await self.client.send_message(self.tg_bot, "/videonotes")
            print("Включено все в ТГ для сообщений миты")
        except Exception as e:
            self.gui.silero_connected.set(False)
            print(f"Ошибка авторизации: {e}")
        
    async def start(self):
        print("Запуск коннектора ТГ!")
        try:
            await self.client.connect()

            # Проверяем, авторизован ли уже клиент
            if not await self.client.is_user_authorized():
                # Создаем окно для ввода кода подтверждения
                code_window = tk.Toplevel()
                code_window.title("Подтверждение Telegram")
                code_window.geometry("300x150")
                code_window.resizable(False, False)

                frame = tk.Frame(code_window)
                frame.place(relx=0.5, rely=0.5, anchor="center")

                code_var = tk.StringVar()

                code_entry = tk.Entry(
                    frame, textvariable=code_var, width=20, justify="center"
                )
                code_entry.pack(pady=10)
                code_entry.focus()  # Устанавливаем фокус на поле ввода

                code_future = asyncio.Future()

                def submit_code():
                    if code_var.get().strip():  # Проверяем, что код не пустой
                        code_future.set_result(code_var.get().strip())
                        code_window.destroy()
                    else:
                        tk.messagebox.showerror("Ошибка", "Введите код подтверждения")

                def on_enter(event):
                    submit_code()

                code_entry.bind("<Return>", on_enter)  # Добавляем обработку Enter

                submit_button = tk.Button(
                    frame,
                    text="Подтвердить",
                    command=submit_code,
                    width=15,
                    relief="flat",
                )
                submit_button.pack(pady=15)

                # Ждем код подтверждения
                try:
                    await self.client.sign_in(phone=self.phone)
                    verification_code = await code_future
                    await self.client.sign_in(phone=self.phone, code=verification_code)
                except Exception as e:
                    print(f"Ошибка при вводе кода: {e}")
                    raise

            await self.client.send_message(self.tg_bot, "/start")
            self.gui.silero_connected.set(True)

            if self.tg_bot == "@silero_voice_bot":
                await asyncio.sleep(0.35)
                await self.client.send_message(self.tg_bot, "/speaker mita")
                self.last_speaker_command = "/speaker mita"
                await asyncio.sleep(0.35)
                await self.client.send_message(self.tg_bot, "/mp3")
                await asyncio.sleep(0.35)
                await self.client.send_message(self.tg_bot, "/hd")
                await asyncio.sleep(0.35)
                await self.client.send_message(self.tg_bot, "/videonotes")
            print("Включено все в ТГ для сообщений миты")
        except Exception as e:
            self.gui.silero_connected.set(False)
            print(f"Ошибка авторизации: {e}")
    
    async def play_unread(self, message):
        print(f"Воспроизведение: {message.id}")
        print(f"Текст: {message.id}")
        doc = message.media.document
        # Проверяем MP3-файл
        if "audio/mpeg" in doc.mime_type:
            response = message

        # Проверяем голосовое сообщение (OGG, voice)
        if "audio/ogg" in doc.mime_type:
            for attr in doc.attributes:
                if isinstance(attr, DocumentAttributeAudio) and attr.voice:
                    response = message
                    break
        
        file_path = await self.client.download_media(response.media)
                
        new_file_path = f"{message.reply_to_msg_id}_{file_path}"
        os.rename(file_path, new_file_path)
        file_path = new_file_path

        print(f"Файл загружен: {file_path}")
        sound_absolute_path = os.path.abspath(file_path)
        if self.gui.ConnectedToGame:
            print("Подключен к игре, нужна конвертация")

            # Генерируем путь для WAV-файла на основе имени исходного MP3
            base_name = os.path.splitext(os.path.basename(file_path))[
                0
            ]  # Получаем имя файла без расширения
            wav_path = os.path.join(
                os.path.dirname(file_path), f"{base_name}.wav"
            )  # Создаем новый путь

            # Получаем абсолютный путь

            absolute_wav_path = os.path.abspath(wav_path)
            # Конвертируем MP3 в WAV
            # await  AudioConverter.convert_mp3_to_wav(sound_absolute_path, absolute_wav_path)
            await AudioConverter.convert_to_wav(
                sound_absolute_path, absolute_wav_path
            )
            
            try:
                print(f"Удаляю файл: {sound_absolute_path}")
                os.remove(sound_absolute_path)
                print(f"Файл {sound_absolute_path} удалён.")
            except OSError as remove_error:
                print(
                    f"Ошибка при удалении файла {sound_absolute_path}: {remove_error}"
                )

            self.gui.patch_to_sound_file = absolute_wav_path
            print(f"Файл wav загружен: {absolute_wav_path}")
        else:
            print(f"Отправлен воспроизводится: {sound_absolute_path}")
            await self.handle_voice_file(file_path)
        return
