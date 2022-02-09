import axios from "axios";
import {
  API_HOST_PREFIX,
  onGlobalError,
  onGlobalSuccess,
} from "./serviceHelpers";

const endpoint = `${API_HOST_PREFIX}/api/files`;

var addFile = (payload) => {
  const config = {
    method: "POST",
    url: endpoint,
    data: payload,
    crossdomain: true,
    headers: { "Content-Type": "multipart/form-data" },
  };

  return axios(config).then(onGlobalSuccess).catch(onGlobalError);
};

export { addFile };
