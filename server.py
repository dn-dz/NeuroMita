import json
import socket
from datetime import datetime


class ChatServer:
    def __init__(self, gui, chat_model, host='127.0.0.1', port=12345):
        # Инициализация сервера с GUI, моделью чата и сетевыми параметрами
        self.host = host                        # IP-адрес сервера
        self.port = port                        # Порт сервера
        self.gui = gui                          # Ссылка на графический интерфейс
        self.server_socket = None               # Основной серверный сокет
        self.client_socket = None               # Сокет для активного клиента
        self.passive_client_socket = None       # Резервный клиентский сокет (не используется)
        self.passive_server_socket = None       # Резервный серверный сокет (не используется)
        self.chat_model = chat_model            # Модель обработки сообщений
        self.messages_to_say = []               # Очередь сообщений для отправки

    def start(self):
        """Инициализирует и запускает сервер, создавая TCP-сокет."""
        self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.server_socket.bind((self.host, self.port)) # Привязка к адресу
        self.server_socket.listen(5)                    # Ожидание подключений (макс. 5 в очереди)
        print(f"Сервер запущен на {self.host}:{self.port}")

    #TODO РЕФАКТОРЬ РЕФАКТОРЬ РЕФАКТОРЬ!
    def handle_connection(self):
        """Обрабатывает одно подключение."""
        # Проверка инициализации сервера
        if not self.server_socket:
            raise RuntimeError("Сервер не запущен. Вызовите start() перед handle_connection().")
        try:
            #print("Жду получения от клиента игры")
            # Ожидание подключения
            self.client_socket, addr = self.server_socket.accept()
            #print(f"Подключен {addr}")

            received_text = self.client_socket.recv(8192).decode("utf-8")

            # Логируем полученные данные
            #print(f"Получено: {received_text}")

            # Проверяем, корректно ли закрыт JSON (простая проверка)
            # Валидация JSON структуры
            if not received_text.strip().endswith("}"):
                print("Ошибка: JSON оборван")
                return False
            # Парсинг JSON данных
            try:
                message_data = json.loads(received_text)
                    #print("JSON успешно разобран!")
            except json.JSONDecodeError as e:
                print(f"Ошибка обработки JSON: {e}")
                return False
            # Обработка распарсенных данных
            self.process_message_data(message_data)
            return True

        except socket.error as e:
            print(f"Socket error: {e}")
        except Exception as e:
            print(f"Connection handling error: {e}")
            self.gui.ConnectedToGame = False    # Обновление статуса в GUI
        finally:
            if self.client_socket:
                self.client_socket.close()      # Важно закрывать соединение
            return False

    def process_message_data(self, message_data):
        """Обрабатывает распарсенные данные сообщения. Возвращает статус обработки."""
        transmitted_to_game = False
        try:   
            # Извлечение базовых параметров сообщения
            message_id = message_data["id"]                             # Уникальный идентификатор сообщения
                                         # Уникальный идентификатор сообщения для вывода в окне

            message_type = message_data["type"]                         # Тип сообщения (системное/пользовательское)
            character = str(message_data["character"])                  # Персонаж-отправитель
            self.chat_model.current_character_to_change = character     # Текущий персонаж

            message = message_data["input"]                             # Текст сообщения               
            system_message = message_data["dataToSentSystem"]
            system_info = message_data["systemInfo"]

            # Вот это этого надо будет изьавиться
            self.chat_model.distance = float(message_data["distance"].replace(",", "."))
            self.chat_model.roomPlayer = int(message_data["roomPlayer"])
            self.chat_model.roomMita = int(message_data["roomMita"])
            self.chat_model.nearObjects = message_data["hierarchy"]
            self.chat_model.actualInfo = message_data["currentInfo"]

            if system_info != "-":
                print("Добавил систем инфо " + system_info)
                self.chat_model.add_temporary_system_info(system_info)

            response = ""


            # Обработка системных сообщений
            if message == "waiting":
                if system_message != "-":
                    print(f"Получено system_message {system_message} id {message_id}")
                    self.gui.id_sound = message_id
                    response = self.generate_response("", system_message)
                    self.gui.insertDialog("", response)
                elif self.messages_to_say:

                    response = self.messages_to_say.pop(0)
            elif message == "boring":
                print(f"Получено boring message id {message_id}")
                date_now = datetime.datetime.now().replace(microsecond=0)
                self.gui.id_sound = message_id
                response = self.generate_response("",
                                                  f"Время {date_now}, Игрок долго молчит( Ты можешь что-то сказать или предпринять")
                self.gui.insertDialog("", response)
                print("Отправлено Мите на озвучку: " + response)
            else:
                print(f"Получено message id {message_id}")
                # Если игрок отправил внутри игры, message его
                self.gui.id_sound = message_id
                response = self.generate_response(message, "")
                #self.gui.insertDialog(message,response)
                print("Отправлено Мите на озвучку: " + response)

                if not character:
                    character = "Mita"

                transmitted_to_game = False
                if self.gui.user_input:
                    transmitted_to_game = True

            if self.gui.patch_to_sound_file!="":
                print(f"id {message_id} Скоро передам {self.gui.patch_to_sound_file} id {self.gui.id_sound}")

            message_data = {
            "id": int(message_id),
            "type": str(message_type),
            "character": str(character),
            }
            message_data.update({
                "response": str(response),
                "silero": bool(self.gui.silero_connected and bool(self.gui.settings.get("SILERO_USE"))),
                "id_sound": int(self.gui.id_sound),
                "patch_to_sound_file": str(self.gui.patch_to_sound_file),
                "user_input": str(self.gui.user_input),

                # Простите, но я хотел за вечер затестить
                "GM_ON": bool(self.gui.settings.get("GM_ON")),
                "GM_READ": bool(self.gui.settings.get("GM_READ")),
                "GM_VOICE": bool(self.gui.settings.get("GM_VOICE")),
                "GM_REPEAT": int(self.gui.settings.get("GM_REPEAT")),
                "CC_Limit_mod": int(self.gui.settings.get("CC_Limit_mod"))
            })
            #print(message_data)

            self.gui.patch_to_sound_file = ""

            if transmitted_to_game:
                self.gui.clear_user_input()

            # Отправляем JSON через сокет
            json_message = json.dumps(message_data)
            self.client_socket.send(json_message.encode("utf-8"))

            self.gui.ConnectedToGame = True
            return True
        except Exception as e:
            print(f"Ошибка обработки подключения: {e}")
            self.gui.ConnectedToGame = False
            return False
        finally:
            if self.client_socket:
                self.client_socket.close()

    def generate_response(self, input_text, system_input_text):
        """Генерирует текст с помощью модели."""
        try:

            self.gui.waiting_answer = True
            response = self.chat_model.generate_response(input_text, system_input_text)
            if input_text != "":
                self.gui.insertDialog(input_text, response)

        except Exception as e:
            print(f"Ошибка генерации ответа: {e}")
            response = "Произошла ошибка при обработке вашего сообщения."

            self.gui.waiting_answer = False
        return response

    def send_message_to_server(self, message):
        self.MessagesToSay.append(message)

    def stop(self):
        """Закрывает сервер."""
        if self.server_socket:
            self.server_socket.close()
            print("Сервер остановлен.")
            self.gui.ConnectedToGame = False
