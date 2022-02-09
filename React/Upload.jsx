mport React, { Fragment, Component } from "react";
import { Grid } from "@material-ui/core";
import CloudUploadIcon from "@material-ui/icons/CloudUpload";
import Dropzone from "react-dropzone";
import * as fileService from "../../services/fileService";
import toastr from "toastr";
import PropTypes from "prop-types";

import debug from "sabio-debug";
const _logger = debug.extend("Files");

export default class FileUpload extends Component {
  constructor(props) {
    super(props);
    this.state = {
      files: [],
    };
  }

  onDrop(files) {
    this.setState({ files });

    var formData = new FormData();
    for (let i = 0; i < files.length; i++) {
      const currentFile = files[i];
      formData.append("formFiles", currentFile);
    }
    fileService
      .addFile(formData)
      .then(this.onAddFileSuccess)
      .catch(this.onAddFileError);
  }

  onCancel() {
    this.setState({
      files: [],
    });
  }

  onAddFileSuccess = (response) => {
    _logger("onAddFileSuccess", response);
    toastr.success("You have successfully added a file");
    this.props.handleSuccess(response);
  };

  onAddFileError = (err) => {
    _logger("onAddFileError", err);
    toastr.error("Failed to upload a file");
  };

  render() {
    return (
      <Fragment>
        <Grid container className="p-3">
          <Grid item xs={12} sm={12} md={12}>
            <Dropzone
              onDrop={this.onDrop.bind(this)}
              onFileDialogCancel={this.onCancel.bind(this)}
              multiple={this.props.multiple}
            >
              {({ getRootProps, getInputProps }) => (
                <div {...getRootProps()}>
                  <input {...getInputProps()} />
                  <div className="dz-message">
                    <div className="dx-text">
                      {this.props.multiple ? (
                        <div className="text-center text-primary">
                          <CloudUploadIcon
                            height="100px"
                            className="font-size-xl text-primary"
                          />{" "}
                          Upload files
                        </div>
                      ) : (
                        <div className="text-center text-primary">
                          <CloudUploadIcon
                            height="100px"
                            className="font-size-xl text-primary"
                          />{" "}
                          Upload a file
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              )}
            </Dropzone>
          </Grid>
        </Grid>
      </Fragment>
    );
  }
}

FileUpload.propTypes = {
  handleSuccess: PropTypes.func.isRequired,
  multiple: PropTypes.bool.isRequired,
};
