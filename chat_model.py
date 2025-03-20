import time

import tiktoken
from openai import OpenAI
from g4f.client import Client as g4fClient
#from huggingface_hub import HfApi
from mistralai import Mistral as MistralClient

import re

from character import *
from utils import *
# Настройка логирования
import logging
import colorlog

# Настройка цветного логирования
handler = colorlog.StreamHandler()
handler.setFormatter(colorlog.ColoredFormatter(
    '%(log_color)s%(asctime)s - %(levelname)s - %(message)s',
    log_colors={
        'INFO': 'white',
        'WARNING': 'yellow',
        'ERROR': 'red',
        'CRITICAL': 'red,bg_white',
    }
))

logger = colorlog.getLogger(__name__)
logger.setLevel(logging.INFO)
logger.addHandler(handler)


class ChatModel:
    def __init__(self, gui, api_key, api_key_res, api_url, api_model, gpt4free_model, api_make_request):

        # Временное решение, чтобы возвращать работоспособность старого формата

        self.OldSystem = False

        self.gui = gui

        try:
            self.api_key = api_key
            self.api_key_res = api_key_res
            self.api_url = api_url
            self.api_model = api_model
            self.gpt4free_model = gpt4free_model
            self.makeRequest = api_make_request

            self.g4fClient = g4fClient()
            logger.info(f"g4fClient успешно инициализирован. Какой же кайф, будто бы теперь без None живем")

            #self.mistral_client = MistralClient()

            self.client = OpenAI(api_key=self.api_key, base_url=self.api_url)

            #self.hugging_face_client = HfApi()
            print("Со старта удалось запустить OpenAi client")
        except:
            print("Со старта не получилось запустить OpenAi client")

        try:
            self.tokenizer = tiktoken.encoding_for_model("gpt-4o-mini")
            self.hasTokenizer = True
        except:
            print("Тиктокен не сработал( Ну и пофиг, на билдах он никогда и не работал")
            self.hasTokenizer = False

        self.max_response_tokens = 3200

        """ Очень спорно уже """
        self.cost_input_per_1000 = 0.0432
        self.cost_response_per_1000 = 0.1728
        """"""

        self.memory_limit = int(self.gui.settings.get("MODEL_MESSAGE_LIMIT", 40))  # Ограничение на сообщения

        """New System"""
        self.current_character = None
        self.current_character_to_change = str(self.gui.settings.get("CHARACTER"))
        self.characters = None

        """То, что нужно будет убрать в одну переменную"""

        self.distance = 0.0
        self.roomPlayer = -1
        self.roomMita = -1

        self.nearObjects = ""
        self.actualInfo = ""

        """То, что нужно будет убрать в одну переменную"""

        self.LongMemoryRememberCount = 0

        self.infos = []

        # Загрузка данных из файлов

        self.init_characters()

        self.HideAiData = True

        # Настройки реквестов
        self.max_request_attempts = int(self.gui.settings.get("MODEL_MESSAGE_ATTEMPTS_COUNT", 3))
        self.request_delay = float(self.gui.settings.get("MODEL_MESSAGE_ATTEMPTS_TIME", 0.20))

    def init_characters(self):
        """
        Инициализирует возможных персонажей
        """
        self.crazy_mita_character = CrazyMita("Crazy", "/speaker mita", True)
        self.cappy_mita_character = CappyMita("Cappy", "/speaker cap", True)
        self.cart_space = SpaceCartridge("Cart_portal", "/speaker  wheatley", True)
        self.kind_mita_character = KindMita("Kind", "/speaker kind", True)
        self.shorthair_mita_character = ShortHairMita("ShortHair", "/speaker  shorthair", True)
        self.mila_character = MilaMita("Mila", "/speaker mila", True)
        self.sleepy_character = SleepyMita("Sleepy", "/speaker dream", True)
        self.cart_divan = DivanCartridge("Cart_divan", "/speaker engineer", True)
        self.creepy_character = CreepyMita("Creepy", "/speaker ghost", True)  #Спикер на рандом поставил
        self.GameMaster = GameMaster()  # Спикер на рандом поставил

        # Словарь для сопоставления имен персонажей с их объектами
        self.characters = {
            self.crazy_mita_character.name: self.crazy_mita_character,
            self.kind_mita_character.name: self.kind_mita_character,
            self.cappy_mita_character.name: self.cappy_mita_character,
            self.cart_space.name: self.cart_space,
            self.cart_divan.name: self.cart_divan,
            self.shorthair_mita_character.name: self.shorthair_mita_character,
            self.mila_character.name: self.mila_character,
            self.sleepy_character.name: self.sleepy_character,
            self.creepy_character.name: self.creepy_character,
            self.GameMaster.name: self.GameMaster
        }

        self.current_character = self.crazy_mita_character

    def get_all_mitas(self):
        print(f"Characters {self.characters.keys()}")
        return list(self.characters.keys())

    def update_openai_client(self, reserve_key=False):
        print("Попытка обновить клиент")
        if reserve_key and self.api_key_res != "":
            print("С резервным ключом")
            key = self.api_key_res
        else:
            print("С основным ключом")
            key = self.api_key

        try:
            if self.api_url != "":
                print("И ключ и ссылка")
                self.client = OpenAI(api_key=key,
                                     base_url=self.api_url)
            else:
                print("Только ключ")
                self.client = OpenAI(api_key=key)
        except Exception as e:
            print(f"update_openai_client не сработал {e}")

    def generate_response(self, user_input, system_input=""):
        # Загрузка истории из файла
        self.check_change_current_character()

        data = self.current_character.load_history()
        messages = data.get("messages", [])
        if len(self.infos) > 0:
            print("Попытался расширить messages")
            messages.extend(self.infos)
            self.infos.clear()
        self.current_character.process_logic(messages)

        # Добавление информации о времени и пользовательского ввода

        messages = self.current_character.add_context(messages)
        messages = self._add_input(user_input, system_input, messages)

        # Ограничение на количество сообщений
        messages = messages[-self.memory_limit:]

        # Обновление текущего настроения
        timed_system_message = self.current_character.current_variables()

        combined_messages, messages = self._combine_messages_character(self.current_character, messages,
                                                                       timed_system_message)

        # Генерация ответа с использованием клиента
        try:

            response, success = self._generate_chat_response(combined_messages)

            if not success:
                print("Неудачная генерация")
                return response
            elif response == "":
                print("Пустая генерация")
                return response

            response_message = {
                "role": "assistant",
                "content": response
            }
            messages.append(response_message)

            # Процессинг ответа: изменяем показатели и сохраняем историю
            response = self.current_character.process_response(response)

            print(f"До фразы {response}")

            if self.current_character == self.GameMaster and not bool(self.gui.settings.get("GM_VOICE")):
                pass
            else:
                self.gui.textToTalk = self.process_text_to_voice(response)
                self.gui.textSpeaker = self.current_character.silero_command
                self.gui.silero_turn_off_video = self.current_character.silero_turn_off_video
                print("self.gui.textToTalk: " + self.gui.textToTalk)
                print("self.gui.textSpeaker: " + self.gui.textSpeaker)

            self.current_character.safe_history(messages, timed_system_message)

            self.gui.update_debug_info()
            return response
        except Exception as e:
            logger.error(f"Ошибка на фазе генерации: {e}", exc_info=True)
            return f"Ошибка на фазе генерации: {e}"

    def save_chat_history(self):
        self.current_character.safe_history()

    def check_change_current_character(self):
        """
        Проверяет и изменяет текущего персонажа на основе значения `current_character_to_change`.

        Если `current_character_to_change` соответствует имени одного из персонажей,
        текущий персонаж (`current_character`) обновляется, а `current_character_to_change` сбрасывается.
        """
        if not self.current_character_to_change:
            return  # Если строка пустая, ничего не делаем

        # Проверяем, есть ли имя в словаре
        if self.current_character_to_change in self.characters:
            print(f"Меняю персонажа на {self.current_character_to_change}")
            self.current_character = self.characters[self.current_character_to_change]
            self.current_character_to_change = ""  # Сбрасываем значение

    def _add_input(self, user_input, system_input, messages):
        """Добавляет то самое последнее сообщение"""

        if system_input != "":
            messages.append({"role": "system", "content": system_input})
        if user_input != "":
            messages.append({"role": "user", "content": user_input})
        return messages

    def get_room_name(self, room_id):
        # Сопоставление ID комнаты с её названием
        room_names = {
            0: "Кухня",  # Кухня
            1: "Зал",  # Главная комната
            2: "Комната",  # Спальня
            3: "Туалет",  # Туалет
            4: "Подвал"  # Подвал
        }

        # Возвращаем название комнаты, если оно есть, иначе возвращаем сообщение о неизвестной комнате
        return room_names.get(room_id, "?")

    def _combine_messages_character(self, character: Character, messages, timed_system_message):
        """Комбинирование всех сообщений перед отправкой"""
        # Чем выше здесь, тем дальше от начала будет

        combined_messages = character.prepare_fixed_messages()

        # Добавляем timed_system_message, если это словарь
        if isinstance(timed_system_message, dict) and timed_system_message["content"] != "":
            combined_messages.append(timed_system_message)
            print("timed_system_message успешно добавлено.")

        if self.nearObjects != "" and self.nearObjects != "-":
            text = f"В радиусе от тебя следующие объекты (object tree) {self.nearObjects}"
            messageNear = {"role": "system", "content": text}
            combined_messages.append(messageNear)

        if self.actualInfo != "" and self.actualInfo != "-":
            messageActual = {"role": "system", "content": self.actualInfo}
            combined_messages.append(messageActual)

        # Добавляем messages, если они не пустые
        if messages:
            combined_messages.extend(messages)
            print("messages успешно добавлены. Количество:", len(messages))
        messages = character.prepare_float_messages(messages)

        #combined_messages = character.add_context(combined_messages)

        return combined_messages, messages

    def _generate_chat_response(self, combined_messages):
        """Генерирует ответ с использованием единого цикла"""
        max_attempts = self.max_request_attempts  # Общее максимальное количество попыток
        retry_delay = self.request_delay  # Задержка между попытками в секундах
        used_reserve_key = False  # Флаг использования резервного ключа

        # Определяем провайдера для первой попытки
        use_gemini = self.makeRequest and not bool(self.gui.settings.get("gpt4free"))

        for attempt in range(1, max_attempts + 1):
            print(f"Попытка генерации {attempt}/{max_attempts}")
            response = None

            # Логируем начало генерации
            self._log_generation_start()
            save_combined_messages(combined_messages)

            try:
                # Выбираем провайдера
                if use_gemini:  # Gemini только на первой попытке
                    formatted = self._format_messages_for_gemini(combined_messages)
                    response = self._generate_gemini_response(formatted)
                else:
                    # Переключаем ключи начиная со второй попытки
                    if attempt > 1:
                        self.update_openai_client(reserve_key=not used_reserve_key)
                        used_reserve_key = not used_reserve_key

                    response = self._generate_openai_response(combined_messages)

                if response:
                    response = self._clean_response(response)
                    #logger.info(f"Успешный ответ:\n{response}")
                    return response, True

            except Exception as e:
                logger.error(f"Ошибка генерации: {str(e)}")

            # Если ответа нет - ждем перед следующей попыткой
            if attempt < max_attempts and not used_reserve_key:
                print(f"Ожидание {retry_delay} сек. перед повторной попыткой...")
                time.sleep(retry_delay)

        print("Все попытки исчерпаны")
        return None, False

    def _log_generation_start(self):
        logger.info("Перед отправкой на генерацию")
        logger.info(f"API Key: {SH(self.api_key)}")
        logger.info(f"API Key res: {SH(self.api_key_res)}")
        logger.info(f"API URL: {self.api_url}")
        logger.info(f"API Model: {self.api_model}")
        logger.info(f"Make Request: {self.makeRequest}")

    def _format_messages_for_gemini(self, combined_messages):
        #TODO Надо кароче первые сообщения сделать системными

        formatted_messages = []
        for msg in combined_messages:
            if msg["role"] == "system":
                formatted_messages.append({"role": "user", "content": f"[System Prompt]: {msg['content']}"})
            else:
                formatted_messages.append(msg)
        save_combined_messages(formatted_messages, "Gem")
        return formatted_messages

    def _generate_gemini_response(self, formatted_messages):
        try:
            response = self.generate_responseGemini(formatted_messages)
            logger.info(f"Ответ Gemini: {response}", )
            return response
        except Exception as e:
            logger.error("Что-то не так при генерации Gemini", str(e))
            return None

    def _generate_openai_response(self, combined_messages):
        if not self.client:
            logger.info("Попытка переподключения клиента")
            self.update_openai_client()

        try:
            if "gemini" in self.api_model and combined_messages[-1]["role"] == "system":
                print("gemini последнее системное сообщение на юзерское")
                combined_messages[-1]["role"] = "user"
                combined_messages[-1]["content"] = "[SYSTEM INFO]" + combined_messages[-1]["content"]

            print(f"Перед запросом  {len(combined_messages)}", )

            if bool(self.gui.settings.get("gpt4free")):
                print("gpt4free case")

                completion = self.g4fClient.chat.completions.create(
                    model=self.gpt4free_model,
                    messages=combined_messages,
                    max_tokens=self.max_response_tokens,
                    temperature=0.5,
                    web_search=False
                )
            else:
                completion = self.client.chat.completions.create(
                    model=self.api_model,
                    messages=combined_messages,
                    max_tokens=self.max_response_tokens,
                    #presence_penalty=1.5,
                    temperature=0.5
                )
            print(f"after completion{completion}")

            if completion:
                if completion.choices:
                    response = completion.choices[0].message.content
                    print(f"response {response}")
                    return response.lstrip("\n")
                else:
                    print("completion.choices пусто")
                    logger.warning(completion)
                    self.try_print_error(completion)
                    return None
            else:
                logger.warning("completion пусто")
                return None

        except Exception as e:
            logger.error("Что-то не так при генерации OpenAI", str(e))
            return None

    def _save_and_calculate_cost(self, combined_messages):
        save_combined_messages(combined_messages)
        try:
            self.gui.last_price = calculate_cost_for_combined_messages(self, combined_messages,
                                                                       self.cost_input_per_1000)
            logger.info(f"Calculated cost: {self.gui.last_price}")
        except Exception as e:
            ...
            logger.info("Не получилось сделать с токенайзером, это скорее всего особенность билда")
            #logger.info("Не получилось сделать с токенайзером", str(e))

    def try_print_error(self, completion):
        try:
            if not completion or not hasattr(completion, 'error'):
                logger.warning("Ошибка: объект completion не содержит информации об ошибке.")
                return

            error = completion.error
            if not error:
                logger.warning("Ошибка: объект completion.error пуст.")
                return

            # Основное сообщение об ошибке

            logger.warning(f"ChatCompletion ошибка: {error}")

            # Дополнительные метаданные об ошибке
            if hasattr(error, 'metadata'):
                metadata = error.metadata
                if metadata:
                    logger.warning("Метаданные ошибки:")
                    if hasattr(metadata, 'raw'):
                        logger.warning(f"Raw данные: {metadata.raw}")
                    if hasattr(metadata, 'provider_name'):
                        logger.warning(f"Провайдер: {metadata.provider_name}")
                    if hasattr(metadata, 'isDownstreamPipeClean'):
                        logger.warning(f"Состояние downstream: {metadata.isDownstreamPipeClean}")
                    if hasattr(metadata, 'isErrorUpstreamFault'):
                        logger.warning(f"Ошибка upstream: {metadata.isErrorUpstreamFault}")
                else:
                    logger.warning("Метаданные ошибки отсутствуют.")
            else:
                logger.warning("Метаданные ошибки недоступны.")

        except Exception as e:
            logger.error(f"Ошибка при попытке обработать ошибку ChatCompletion: {e}")

    def _clean_response(self, response):
        try:
            # Проверяем, что response является строкой
            if not isinstance(response, str):
                logger.warning(f"Ожидалась строка, но получен тип: {type(response)}")
                return response  # Возвращаем исходное значение, если это не строка

            # Убираем префиксы и суффиксы
            if response.startswith("```\n"):
                response = response.lstrip("```\n")
            if response.endswith("\n```\n"):
                response = response.removesuffix("\n```\n")
        except Exception as e:
            logger.error(f"Проблема с префиксами или постфиксами: {e}")
        return response

    def generate_responseGemini(self, combined_messages):
        data = {
            "contents": [
                {"role": msg["role"], "parts": [{"text": msg["content"]}]} for msg in combined_messages
            ],
            "generationConfig": {
                "maxOutputTokens": self.max_response_tokens,
                "temperature": 1,
                "presencePenalty": 1.5
            }
        }

        headers = {
            "Content-Type": "application/json",
            "Authorization": f"Bearer {self.api_key}"
        }

        logger.info("Отправляю запрос к Gemini")
        save_combined_messages(data, "Gem2")
        response = requests.post(self.api_url, headers=headers, json=data)

        if response.status_code == 200:
            response_data = response.json()
            generated_text = response_data.get("candidates", [{}])[0].get("content", {}).get("parts", [{}])[0].get(
                "text", "")
            logger.info("Gemini Flash: \n" + generated_text)
            return generated_text
        else:
            logger.error(f"Ошибка: {response.status_code}, {response.text}")
            return None

    def process_commands(self, response, messages):
        """
        Обрабатывает команды типа <c>...</c> в ответе.
        Команды могут быть: "Достать бензопилу", "Выключить игрока" и другие.
        """
        start_tag = "<c>"
        end_tag = "</c>"
        search_start = 0  # Указатель для поиска новых команд

        while start_tag in response[search_start:] and end_tag in response[search_start:]:
            try:
                # Находим команду
                start_index = response.index(start_tag, search_start) + len(start_tag)
                end_index = response.index(end_tag, start_index)
                command = response[start_index:end_index]

                # Логируем текущую команду
                print(f"Обработка команды: {command}")

                # Обработка команды
                if command == "Достать бензопилу":
                    ...
                    #add_temporary_system_message(messages, "Игрок был не распилен, произошла ошибка")

                    #if self.gui:
                    #   self.gui.close_app()

                elif command == "Выключить игрока":
                    ...
                    #add_temporary_system_message(messages, "Игрок был отпавлен в главное меню, но скоро он вернется...")

                    #if self.gui:
                    #   self.gui.close_app()

                else:
                    # Обработка неизвестных команд
                    #add_temporary_system_message(messages, f"Неизвестная команда: {command}")
                    print(f"Неизвестная команда: {command}")

                # Сдвигаем указатель поиска на следующий символ после текущей команды
                search_start = end_index + len(end_tag)

            except ValueError as e:
                self.add_temporary_system_message(messages, f"Ошибка обработки команды: {e}")
                break

        return response

    def process_text_to_voice(self, text):
        # Проверяем, что текст является строкой (если это байты, декодируем)
        if isinstance(text, bytes):
            try:
                text = text.decode("utf-8")  # Декодируем в UTF-8
            except UnicodeDecodeError:
                # Если UTF-8 не подходит, пробуем определить кодировку
                import chardet
                encoding = chardet.detect(text)["encoding"]
                text = text.decode(encoding)

        # Удаляем все теги и их содержимое
        clean_text = re.sub(r"<[^>]+>.*?</[^>]+>", "", text)
        clean_text = re.sub(r"<.*?>", "", clean_text)
        clean_text = replace_numbers_with_words(clean_text)

        #clean_text = transliterate_english_to_russian(clean_text)

        # Если текст пустой, заменяем его на "Вот"
        if clean_text.strip() == "":
            clean_text = "Вот"

        return clean_text

    def reload_promts(self):
        print("Перезагрузка промптов")

        self.current_character.init()
        self.current_character.process_logic()

    def add_temporary_system_message(self, messages, content):
        """
        Добавляет одноразовое системное сообщение в список сообщений.

        :param messages: Список сообщений, в который добавляется системное сообщение.
        :param content: Текст системного сообщения.
        """
        system_message = {
            "role": "system",
            "content": content
        }
        messages.append(system_message)

    def add_temporary_system_info(self, content):
        """
        Добавляет одноразовое системное сообщение в список сообщений.

        :param messages: Список сообщений, в который добавляется системное сообщение.
        :param content: Текст системного сообщения.
        """
        system_info = {
            "role": "system",
            "content": content
        }
        self.infos.append(system_info)
        #.append(system_message)

    #region TokensCounting
    def calculate_cost(self, user_input):
        # Загружаем историю
        history_data = self.load_history()

        # Получаем только сообщения
        messages = history_data.get('messages', [])

        # Добавляем новое сообщение от пользователя
        messages.append({"role": "user", "content": user_input})

        # Считаем токены
        token_count = self.count_tokens(messages)

        # Рассчитываем стоимость
        cost = (token_count / 1000) * self.cost_input_per_1000

        return token_count, cost

    def count_tokens(self, messages):
        return sum(len(self.tokenizer.encode(msg["content"])) for msg in messages if
                   isinstance(msg, dict) and "content" in msg)

    #endregion
